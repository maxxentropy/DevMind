using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface IAgentOrchestrationService
{
    Task<AgentResponse> ProcessUserRequestAsync(UserRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentSession>> GetSessionHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<AgentResponse> ContinueConversationAsync(UserRequest request, Guid previousSessionId, CancellationToken cancellationToken = default);
}
