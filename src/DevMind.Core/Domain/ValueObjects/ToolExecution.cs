// src/DevMind.Core/Domain/ValueObjects/ToolExecution.cs

namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// Represents the execution context and metadata for a tool call using the Result pattern
/// </summary>
public class ToolExecution
{
    #region Properties

    /// <summary>
    /// The tool call that was executed
    /// </summary>
    public ToolCall ToolCall { get; private set; } = null!;

    /// <summary>
    /// When the execution completed
    /// </summary>
    public DateTime CompletedAt { get; private set; }

    /// <summary>
    /// How long the execution took
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    /// <summary>
    /// The session this execution belongs to
    /// </summary>
    public Guid? SessionId { get; private set; }

    #endregion

    #region Constructors

    private ToolExecution() { } // EF Constructor

    private ToolExecution(
        ToolCall toolCall,
        TimeSpan duration,
        Dictionary<string, object>? metadata = null)
    {
        ToolCall = toolCall ?? throw new ArgumentNullException(nameof(toolCall));
        CompletedAt = DateTime.UtcNow;
        Duration = duration;
        Metadata = metadata ?? new Dictionary<string, object>();
        SessionId = toolCall.SessionId;
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a successful tool execution result
    /// </summary>
    /// <param name="toolCall">The tool call that was executed</param>
    /// <param name="result">The result data from the tool execution</param>
    /// <param name="duration">How long the execution took</param>
    /// <param name="metadata">Additional metadata about the execution</param>
    /// <returns>A successful tool execution result</returns>
    public static Result<ToolExecution> Success(
        ToolCall toolCall,
        object? result = null,
        TimeSpan? duration = null,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        var execution = new ToolExecution(
            toolCall,
            duration ?? TimeSpan.Zero,
            metadata);

        if (result != null)
        {
            execution.Metadata["execution_result"] = result;
        }

        return Result<ToolExecution>.Success(execution);
    }

    /// <summary>
    /// Creates a failed tool execution result
    /// </summary>
    /// <param name="toolCall">The tool call that failed</param>
    /// <param name="errorCode">The error code</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="duration">How long the execution took before failing</param>
    /// <param name="metadata">Additional metadata about the execution</param>
    /// <returns>A failed tool execution result</returns>
    public static Result<ToolExecution> Failure(
        ToolCall toolCall,
        string errorCode,
        string errorMessage,
        TimeSpan? duration = null,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var executionMetadata = metadata ?? new Dictionary<string, object>();
        executionMetadata["tool_name"] = toolCall.ToolName;
        executionMetadata["execution_duration"] = duration ?? TimeSpan.Zero;
        executionMetadata["session_id"] = toolCall.SessionId!;

        var error = ResultError.Create(errorCode, errorMessage, executionMetadata);

        return Result<ToolExecution>.Failure(error);
    }

    /// <summary>
    /// Creates a failed tool execution result from an exception
    /// </summary>
    /// <param name="toolCall">The tool call that failed</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="duration">How long the execution took before failing</param>
    /// <param name="metadata">Additional metadata about the execution</param>
    /// <returns>A failed tool execution result</returns>
    public static Result<ToolExecution> FromException(
        ToolCall toolCall,
        Exception exception,
        TimeSpan? duration = null,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(exception);

        var executionMetadata = metadata ?? new Dictionary<string, object>();
        executionMetadata["tool_name"] = toolCall.ToolName;
        executionMetadata["execution_duration"] = duration ?? TimeSpan.Zero;
        executionMetadata["session_id"] = toolCall.SessionId!;
        executionMetadata["exception_type"] = exception.GetType().Name;

        var errorCode = GetErrorCodeFromException(exception, toolCall.ToolName);
        var error = ResultError.Create(errorCode, exception.Message, executionMetadata)
            .WithMetadata("original_exception", exception.GetType().FullName!);

        return Result<ToolExecution>.Failure(error);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the execution result if it exists
    /// </summary>
    /// <typeparam name="T">The expected type of the result</typeparam>
    /// <returns>The execution result cast to the specified type, or default if not found</returns>
    public T? GetResult<T>()
    {
        if (Metadata.TryGetValue("execution_result", out var result) && result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    /// <summary>
    /// Adds metadata to the execution
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>The same execution for chaining</returns>
    public ToolExecution WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Metadata[key] = value;
        return this;
    }

    #endregion

    #region Private Methods

    private static string GetErrorCodeFromException(Exception exception, string toolName)
    {
        return exception switch
        {
            TimeoutException => ToolErrorCodes.ExecutionTimeout,
            ArgumentException => ToolErrorCodes.InvalidParameters,
            UnauthorizedAccessException => ToolErrorCodes.AccessDenied,
            FileNotFoundException => ToolErrorCodes.ResourceNotFound,
            InvalidOperationException => ToolErrorCodes.InvalidOperation,
            _ => ToolErrorCodes.ExecutionFailed
        };
    }

    #endregion
}
