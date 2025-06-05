// src/DevMind.Core/Application/Interfaces/IAgentOrchestrationService.cs

using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Interface for orchestrating AI agent operations using the Response pattern.
/// Coordinates between intent analysis, planning, execution, and response synthesis.
/// </summary>
public interface IAgentOrchestrationService
{
    /// <summary>
    /// Processes a user request through the complete AI agent pipeline
    /// </summary>
    /// <param name="request">The user request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the agent response or error information</returns>
    Task<Result<AgentResponse>> ProcessUserRequestAsync(UserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves session history for analysis and context
    /// </summary>
    /// <param name="limit">Maximum number of sessions to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing session history or error information</returns>
    Task<Result<IEnumerable<AgentSession>>> GetSessionHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues a conversation from a previous session with context
    /// </summary>
    /// <param name="request">The new user request</param>
    /// <param name="previousSessionId">ID of the previous session to continue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the continued conversation response</returns>
    Task<Result<AgentResponse>> ContinueConversationAsync(UserRequest request, Guid previousSessionId, CancellationToken cancellationToken = default);
}
