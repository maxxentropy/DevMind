using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface ILlmService
{
    Task<UserIntent> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default);
    Task<ExecutionPlan> CreateExecutionPlanAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, CancellationToken cancellationToken = default);
    Task<string> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolResult> results, CancellationToken cancellationToken = default);
    Task<string> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
