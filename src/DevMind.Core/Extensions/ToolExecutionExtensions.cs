using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Extensions;

/// <summary>
/// Extension methods for working with tool execution results.
/// Provides analytics, filtering, and transformation capabilities.
/// </summary>
public static class ToolExecutionExtensions
{
    #region Collection Analysis

    /// <summary>
    /// Converts a collection of tool execution results to a summary
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>A summary of the executions</returns>
    public static ToolExecutionSummary ToSummary(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var resultList = results.ToList();
        var successful = resultList.Where(r => r.IsSuccess).ToList();
        var failed = resultList.Where(r => r.IsFailure).ToList();

        return new ToolExecutionSummary
        {
            TotalExecutions = resultList.Count,
            SuccessfulExecutions = successful.Count,
            FailedExecutions = failed.Count,
            TotalDuration = successful.Sum(r => r.Value.Duration.TotalMilliseconds) +
                           failed.Sum(r => r.Error.Metadata.ContainsKey("execution_duration")
                               ? ((TimeSpan)r.Error.Metadata["execution_duration"]).TotalMilliseconds
                               : 0),
            ErrorCodes = failed.Select(r => r.Error.Code).Distinct().ToList(),
            SuccessRate = resultList.Count > 0 ? (double)successful.Count / resultList.Count : 0,
            ErrorsByCategory = failed.GroupBy(r => ToolErrorCodes.GetCategory(r.Error.Code))
                                   .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Filters tool execution results to only successful ones
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Only the successful executions</returns>
    public static IEnumerable<ToolExecution> GetSuccessful(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(r => r.IsSuccess).Select(r => r.Value);
    }

    /// <summary>
    /// Filters tool execution results to only failed ones
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Only the failed execution errors</returns>
    public static IEnumerable<ResultError> GetFailures(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(r => r.IsFailure).Select(r => r.Error);
    }

    /// <summary>
    /// Groups execution results by tool name
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Results grouped by tool name</returns>
    public static Dictionary<string, List<Result<ToolExecution>>> GroupByTool(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.GroupBy(r => r.IsSuccess
                ? r.Value.ToolCall.ToolName
                : r.Error.Metadata.ContainsKey("tool_name")
                    ? r.Error.Metadata["tool_name"].ToString()!
                    : "unknown")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets execution results for a specific tool
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <param name="toolName">The name of the tool to filter by</param>
    /// <returns>Results for the specified tool</returns>
    public static IEnumerable<Result<ToolExecution>> ForTool(this IEnumerable<Result<ToolExecution>> results, string toolName)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        return results.Where(r =>
            (r.IsSuccess && r.Value.ToolCall.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase)) ||
            (r.IsFailure && r.Error.Metadata.ContainsKey("tool_name") &&
             r.Error.Metadata["tool_name"].ToString()!.Equals(toolName, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Filters results by session ID
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <param name="sessionId">The session ID to filter by</param>
    /// <returns>Results for the specified session</returns>
    public static IEnumerable<Result<ToolExecution>> ForSession(this IEnumerable<Result<ToolExecution>> results, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(r =>
            (r.IsSuccess && r.Value.SessionId == sessionId) ||
            (r.IsFailure && r.Error.Metadata.ContainsKey("session_id") &&
             r.Error.Metadata["session_id"].Equals(sessionId)));
    }

    #endregion

    #region Performance Analysis

    /// <summary>
    /// Gets performance metrics for successful executions
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Performance metrics</returns>
    public static ToolPerformanceMetrics GetPerformanceMetrics(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var successful = results.GetSuccessful().ToList();

        if (!successful.Any())
        {
            return new ToolPerformanceMetrics();
        }

        var durations = successful.Select(e => e.Duration.TotalMilliseconds).ToList();

        return new ToolPerformanceMetrics
        {
            TotalExecutions = successful.Count,
            AverageExecutionTime = durations.Average(),
            MinExecutionTime = durations.Min(),
            MaxExecutionTime = durations.Max(),
            MedianExecutionTime = GetMedian(durations),
            StandardDeviation = GetStandardDeviation(durations),
            ToolBreakdown = successful.GroupBy(e => e.ToolCall.ToolName)
                                    .ToDictionary(g => g.Key, g => new ToolMetrics
                                    {
                                        ExecutionCount = g.Count(),
                                        AverageTime = g.Average(e => e.Duration.TotalMilliseconds),
                                        TotalTime = g.Sum(e => e.Duration.TotalMilliseconds)
                                    })
        };
    }

    /// <summary>
    /// Gets performance metrics for a specific time window
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <param name="timeWindow">The time window to analyze</param>
    /// <returns>Performance metrics for the specified time window</returns>
    public static ToolPerformanceMetrics GetPerformanceMetrics(this IEnumerable<Result<ToolExecution>> results, TimeSpan timeWindow)
    {
        ArgumentNullException.ThrowIfNull(results);

        var cutoffTime = DateTime.UtcNow - timeWindow;
        var recentResults = results.Where(r =>
            (r.IsSuccess && r.Value.CompletedAt >= cutoffTime) ||
            (r.IsFailure && r.Error.Timestamp >= cutoffTime));

        return recentResults.GetPerformanceMetrics();
    }

    #endregion

    #region Error Analysis

    /// <summary>
    /// Analyzes error patterns in execution results
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Error analysis</returns>
    public static ToolErrorAnalysis AnalyzeErrors(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var failures = results.GetFailures().ToList();
        var totalCount = results.Count();

        if (!failures.Any())
        {
            return new ToolErrorAnalysis
            {
                TotalErrors = 0,
                ErrorRate = 0,
                RetryableErrors = 0
            };
        }

        return new ToolErrorAnalysis
        {
            TotalErrors = failures.Count,
            ErrorsByCode = failures.GroupBy(e => e.Code)
                                  .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByCategory = failures.GroupBy(e => ToolErrorCodes.GetCategory(e.Code))
                                     .ToDictionary(g => g.Key, g => g.Count()),
            RetryableErrors = failures.Count(e => ToolErrorCodes.IsRetryable(e.Code)),
            MostCommonError = failures.GroupBy(e => e.Code)
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault()?.Key ?? string.Empty,
            ErrorRate = totalCount > 0 ? (double)failures.Count / totalCount : 0
        };
    }

    #endregion

    #region Retry Logic

    /// <summary>
    /// Filters execution results to only retryable failures
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>Only the retryable failed executions</returns>
    public static IEnumerable<ResultError> GetRetryableFailures(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.GetFailures().Where(error => ToolErrorCodes.IsRetryable(error.Code));
    }

    /// <summary>
    /// Determines if a result collection has any retryable failures
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <returns>True if there are retryable failures</returns>
    public static bool HasRetryableFailures(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.GetRetryableFailures().Any();
    }

    /// <summary>
    /// Gets retry candidates with their recommended retry delay
    /// </summary>
    /// <param name="results">The tool execution results</param>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts to consider</param>
    /// <returns>Retry candidates with recommended delays</returns>
    public static IEnumerable<RetryCandidate> GetRetryCandidates(this IEnumerable<Result<ToolExecution>> results, int maxRetryAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.GetRetryableFailures()
                     .Select(error => new RetryCandidate
                     {
                         Error = error,
                         RecommendedDelay = CalculateRetryDelay(error, 1), // First retry
                         MaxAttempts = maxRetryAttempts
                     });
    }

    #endregion

    #region Transformation and Conversion

    /// <summary>
    /// Extracts successful execution data as a typed collection
    /// </summary>
    /// <typeparam name="T">The expected type of execution results</typeparam>
    /// <param name="results">The tool execution results</param>
    /// <returns>Typed collection of successful execution results</returns>
    public static IEnumerable<T> ExtractResults<T>(this IEnumerable<Result<ToolExecution>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.GetSuccessful()
                     .Select(e => e.GetResult<T>())
                     .Where(r => r != null)
                     .Cast<T>();
    }

    #endregion

    #region Private Helper Methods

    private static double GetMedian(IList<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;

        if (count == 0) return 0;
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    private static double GetStandardDeviation(IList<double> values)
    {
        if (values.Count <= 1) return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(x => Math.Pow(x - average, 2));

        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    private static TimeSpan CalculateRetryDelay(ResultError error, int attemptNumber)
    {
        // Base delay with exponential backoff
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber - 1));

        // Error-specific adjustments
        var multiplier = error.Code switch
        {
            ToolErrorCodes.ExecutionTimeout => 2.0,
            ToolErrorCodes.NetworkError => 1.5,
            ToolErrorCodes.ServiceUnavailable => 2.0,
            _ => 1.0
        };

        var finalDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * multiplier);

        // Cap at maximum delay
        return finalDelay > TimeSpan.FromMinutes(2)
            ? TimeSpan.FromMinutes(2)
            : finalDelay;
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Represents a candidate for retry with recommended parameters
/// </summary>
public class RetryCandidate
{
    /// <summary>
    /// The error that can be retried
    /// </summary>
    public ResultError Error { get; set; } = null!;

    /// <summary>
    /// Recommended delay before retry
    /// </summary>
    public TimeSpan RecommendedDelay { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Additional retry metadata
    /// </summary>
    public Dictionary<string, object> RetryMetadata { get; set; } = new();
}

/// <summary>
/// Summary of tool execution results
/// </summary>
public class ToolExecutionSummary
{
    /// <summary>
    /// Total number of tool executions attempted
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public double TotalDuration { get; set; }

    /// <summary>
    /// Unique error codes encountered
    /// </summary>
    public List<string> ErrorCodes { get; set; } = new();

    /// <summary>
    /// Success rate as a percentage (0.0 to 1.0)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution time per tool
    /// </summary>
    public double AverageExecutionTime => TotalExecutions > 0 ? TotalDuration / TotalExecutions : 0;

    /// <summary>
    /// Errors grouped by category
    /// </summary>
    public Dictionary<ToolErrorCategory, int> ErrorsByCategory { get; set; } = new();
}

/// <summary>
/// Performance metrics for tool executions
/// </summary>
public class ToolPerformanceMetrics
{
    /// <summary>
    /// Total number of successful executions analyzed
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTime { get; set; }

    /// <summary>
    /// Minimum execution time in milliseconds
    /// </summary>
    public double MinExecutionTime { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public double MaxExecutionTime { get; set; }

    /// <summary>
    /// Median execution time in milliseconds
    /// </summary>
    public double MedianExecutionTime { get; set; }

    /// <summary>
    /// Standard deviation of execution times
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Performance metrics broken down by tool
    /// </summary>
    public Dictionary<string, ToolMetrics> ToolBreakdown { get; set; } = new();
}

/// <summary>
/// Metrics for a specific tool
/// </summary>
public class ToolMetrics
{
    /// <summary>
    /// Number of executions for this tool
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageTime { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public double TotalTime { get; set; }
}

/// <summary>
/// Error analysis for tool executions
/// </summary>
public class ToolErrorAnalysis
{
    /// <summary>
    /// Total number of errors
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Errors grouped by error code
    /// </summary>
    public Dictionary<string, int> ErrorsByCode { get; set; } = new();

    /// <summary>
    /// Errors grouped by category
    /// </summary>
    public Dictionary<ToolErrorCategory, int> ErrorsByCategory { get; set; } = new();

    /// <summary>
    /// Number of retryable errors
    /// </summary>
    public int RetryableErrors { get; set; }

    /// <summary>
    /// Most frequently occurring error code
    /// </summary>
    public string MostCommonError { get; set; } = string.Empty;

    /// <summary>
    /// Overall error rate (0.0 to 1.0)
    /// </summary>
    public double ErrorRate { get; set; }
}

#endregion
