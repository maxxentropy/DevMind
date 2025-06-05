using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Interface for synthesizing responses from user intents, execution plans, and tool results.
/// Handles the final step of converting raw execution results into user-friendly responses.
/// </summary>
public interface ISynthesisService
{
    /// <summary>
    /// Synthesizes a comprehensive response based on user intent, execution plan, and tool results
    /// </summary>
    /// <param name="intent">The original user intent that initiated the workflow</param>
    /// <param name="plan">The execution plan that was followed</param>
    /// <param name="results">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the synthesized agent response or error information</returns>
    Task<Result<AgentResponse>> SynthesizeResponseAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolExecution> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats an error response for user consumption
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="intent">Optional user intent for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the formatted error response</returns>
    Task<Result<string>> FormatErrorResponseAsync(
        string error,
        UserIntent? intent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats a clarification request when user intent is ambiguous
    /// </summary>
    /// <param name="intent">The user intent that needs clarification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the formatted clarification request</returns>
    Task<Result<string>> FormatClarificationRequestAsync(
        UserIntent intent,
        CancellationToken cancellationToken = default);
}
