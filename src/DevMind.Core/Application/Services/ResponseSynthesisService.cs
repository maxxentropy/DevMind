using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.Core.Application.Services;

public class ResponseSynthesisService : ISynthesisService
{
    // TODO: Implement class members
    
    public ResponseSynthesisService()
    {
        // TODO: Constructor implementation
    }

    public Task<string> FormatClarificationRequestAsync(UserIntent intent, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> FormatErrorResponseAsync(string error, UserIntent? intent = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<AgentResponse> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolResult> results, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
