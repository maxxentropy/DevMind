using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.TestUtilities.Mocks;

public class MockLlmService : ILlmService
{
    // TODO: Implement class members
    
    public MockLlmService()
    {
        // TODO: Constructor implementation
    }

    public Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<ExecutionPlan>> CreateExecutionPlanAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<string>> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolResult> results, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
