using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.Core.Application.Services;

public class IntentAnalysisService : IIntentAnalysisService
{
    // TODO: Implement class members
    
    public IntentAnalysisService()
    {
        // TODO: Constructor implementation
    }

    public Task<UserIntent> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ConfidenceLevel> ValidateIntentAsync(UserIntent intent, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
