using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface ISynthesisService
{
    Task<AgentResponse> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolResult> results, CancellationToken cancellationToken = default);
    Task<string> FormatErrorResponseAsync(string error, UserIntent? intent = null, CancellationToken cancellationToken = default);
    Task<string> FormatClarificationRequestAsync(UserIntent intent, CancellationToken cancellationToken = default);
}
