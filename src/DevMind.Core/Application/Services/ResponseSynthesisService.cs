// src/DevMind.Core/Application/Services/ResponseSynthesisService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.Core.Application.Services;

/// <summary>
/// Service responsible for synthesizing responses from user intents, execution plans, and tool results.
/// Converts raw execution data into user-friendly responses using domain logic.
/// </summary>
public class ResponseSynthesisService : ISynthesisService
{
    #region Private Fields

    private readonly ILogger<ResponseSynthesisService> _logger;

    #endregion

    #region Constructor

    public ResponseSynthesisService(ILogger<ResponseSynthesisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region ISynthesisService Implementation

    /// <summary>
    /// Synthesizes a comprehensive response based on user intent, execution plan, and tool results
    /// </summary>
    /// <param name="intent">The original user intent that initiated the workflow</param>
    /// <param name="plan">The execution plan that was followed</param>
    /// <param name="results">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the synthesized agent response or error information</returns>
    public async Task<Result<AgentResponse>> SynthesizeResponseAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolExecution> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(results);

        try
        {
            _logger.LogDebug("Synthesizing response for intent: {IntentType}", intent.Type);

            var resultsList = results.ToList();
            var successfulResults = resultsList.Where(r => r != null).ToList();
            var failedCount = resultsList.Count - successfulResults.Count;

            // Build response content based on intent type and results
            var responseContent = await BuildResponseContentAsync(intent, plan, successfulResults, cancellationToken);

            // Determine response type based on execution results
            var responseType = DetermineResponseType(plan, successfulResults, failedCount);

            var agentResponse = AgentResponse.CreateSuccess(responseContent, responseType)
                .WithMetadata("intent_type", intent.Type.ToString())
                .WithMetadata("execution_plan_id", plan.Id.ToString())
                .WithMetadata("tool_executions_count", successfulResults.Count)
                .WithMetadata("failed_executions_count", failedCount)
                .WithMetadata("execution_duration", plan.CompletedAt?.Subtract(plan.CreatedAt).ToString() ?? "Unknown");

            _logger.LogDebug("Successfully synthesized response for intent: {IntentType}", intent.Type);

            return Result<AgentResponse>.Success(agentResponse);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Response synthesis was cancelled");
            return Result<AgentResponse>.Failure(
                "SYNTHESIS_CANCELLED",
                "Response synthesis was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing response for intent: {IntentType}", intent.Type);
            return Result<AgentResponse>.Failure(
                "SYNTHESIS_ERROR",
                "Failed to synthesize response",
                new { IntentType = intent.Type, ErrorType = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Formats an error response for user consumption
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="intent">Optional user intent for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the formatted error response</returns>
    public async Task<Result<string>> FormatErrorResponseAsync(
        string error,
        UserIntent? intent = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        try
        {
            await Task.CompletedTask; // Placeholder for async processing

            var contextualMessage = intent?.Type switch
            {
                IntentType.AnalyzeCode => "I encountered an issue while analyzing your code.",
                IntentType.CreateBranch => "I couldn't create the branch as requested.",
                IntentType.RunTests => "There was a problem running the tests.",
                IntentType.GenerateDocumentation => "I had trouble generating the documentation.",
                IntentType.RefactorCode => "The code refactoring encountered an error.",
                IntentType.FindBugs => "The bug analysis couldn't be completed.",
                IntentType.OptimizePerformance => "Performance optimization failed.",
                IntentType.SecurityScan => "The security scan encountered an issue.",
                _ => "I encountered an issue while processing your request."
            };

            var formattedResponse = $"{contextualMessage} Error details: {error}";

            _logger.LogDebug("Formatted error response for intent: {IntentType}", intent?.Type);

            return Result<string>.Success(formattedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting error response");
            return Result<string>.Failure(
                "ERROR_FORMATTING_FAILED",
                "Failed to format error response");
        }
    }

    /// <summary>
    /// Formats a clarification request when user intent is ambiguous
    /// </summary>
    /// <param name="intent">The user intent that needs clarification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the formatted clarification request</returns>
    public async Task<Result<string>> FormatClarificationRequestAsync(
        UserIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        try
        {
            await Task.CompletedTask; // Placeholder for async processing

            var clarificationMessage = intent.Confidence switch
            {
                ConfidenceLevel.Low => GenerateLowConfidenceClarification(intent),
                ConfidenceLevel.Medium => GenerateMediumConfidenceClarification(intent),
                _ => GenerateGenericClarification(intent)
            };

            _logger.LogDebug("Generated clarification request for intent: {IntentType}", intent.Type);

            return Result<string>.Success(clarificationMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting clarification request");
            return Result<string>.Failure(
                "CLARIFICATION_FORMATTING_FAILED",
                "Failed to format clarification request");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds the main response content based on intent and execution results
    /// </summary>
    /// <param name="intent">The user intent</param>
    /// <param name="plan">The execution plan</param>
    /// <param name="results">Successful tool execution results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The formatted response content</returns>
    private async Task<string> BuildResponseContentAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IList<ToolExecution> results,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder for async processing

        var responseBuilder = new System.Text.StringBuilder();

        // Add intent-specific introduction
        responseBuilder.AppendLine(GetIntentIntroduction(intent));

        // Add execution summary
        if (results.Any())
        {
            responseBuilder.AppendLine($"\nI successfully executed {results.Count} operation(s):");

            foreach (var result in results.Take(5)) // Limit to first 5 for brevity
            {
                var toolResult = result.GetResult<object>();
                var summary = toolResult?.ToString() ?? "Operation completed successfully";
                responseBuilder.AppendLine($"• {result.ToolCall.ToolName}: {TruncateString(summary, 100)}");
            }

            if (results.Count > 5)
            {
                responseBuilder.AppendLine($"• ... and {results.Count - 5} more operations");
            }
        }
        else
        {
            responseBuilder.AppendLine("\nNo tools were executed for this request.");
        }

        // Add execution time if available
        if (plan.CompletedAt.HasValue)
        {
            var duration = plan.CompletedAt.Value.Subtract(plan.CreatedAt);
            responseBuilder.AppendLine($"\nCompleted in {duration.TotalSeconds:F1} seconds.");
        }

        return responseBuilder.ToString().Trim();
    }

    /// <summary>
    /// Determines the appropriate response type based on execution results
    /// </summary>
    /// <param name="plan">The execution plan</param>
    /// <param name="successfulResults">Successful execution results</param>
    /// <param name="failedCount">Number of failed executions</param>
    /// <returns>The appropriate response type</returns>
    private static ResponseType DetermineResponseType(ExecutionPlan plan, IList<ToolExecution> successfulResults, int failedCount)
    {
        return plan.Status switch
        {
            ExecutionStatus.Completed when failedCount == 0 => ResponseType.Success,
            ExecutionStatus.Completed when failedCount > 0 && successfulResults.Any() => ResponseType.Warning,
            ExecutionStatus.Failed => ResponseType.Error,
            _ => ResponseType.Information
        };
    }

    /// <summary>
    /// Gets an appropriate introduction based on the user intent
    /// </summary>
    /// <param name="intent">The user intent</param>
    /// <returns>Introduction text</returns>
    private static string GetIntentIntroduction(UserIntent intent)
    {
        return intent.Type switch
        {
            IntentType.AnalyzeCode => "I've analyzed your code and here are the results:",
            IntentType.CreateBranch => "I've processed your branch creation request:",
            IntentType.RunTests => "I've executed the test suite:",
            IntentType.GenerateDocumentation => "I've generated documentation for your code:",
            IntentType.RefactorCode => "I've completed the code refactoring:",
            IntentType.FindBugs => "I've performed a bug analysis:",
            IntentType.OptimizePerformance => "I've analyzed performance optimization opportunities:",
            IntentType.SecurityScan => "I've completed the security scan:",
            _ => "I've processed your request:"
        };
    }

    /// <summary>
    /// Generates clarification message for low confidence intents
    /// </summary>
    /// <param name="intent">The user intent</param>
    /// <returns>Clarification message</returns>
    private static string GenerateLowConfidenceClarification(UserIntent intent)
    {
        return $"I'm not entirely sure what you'd like me to do with '{intent.OriginalRequest}'. " +
               "Could you please provide more specific details about what you're trying to accomplish?";
    }

    /// <summary>
    /// Generates clarification message for medium confidence intents
    /// </summary>
    /// <param name="intent">The user intent</param>
    /// <returns>Clarification message</returns>
    private static string GenerateMediumConfidenceClarification(UserIntent intent)
    {
        return $"I think you want me to {intent.Type.ToString().ToLowerInvariant().Replace("code", " code")} " +
               $"based on '{intent.OriginalRequest}'. Is this correct, or did you have something else in mind?";
    }

    /// <summary>
    /// Generates generic clarification message
    /// </summary>
    /// <param name="intent">The user intent</param>
    /// <returns>Clarification message</returns>
    private static string GenerateGenericClarification(UserIntent intent)
    {
        return $"Could you clarify what you'd like me to do with '{intent.OriginalRequest}'? " +
               "Please provide more specific instructions.";
    }

    /// <summary>
    /// Truncates a string to a specified length with ellipsis
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length</param>
    /// <returns>Truncated string</returns>
    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
