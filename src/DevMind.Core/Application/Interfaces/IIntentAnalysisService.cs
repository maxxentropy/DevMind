using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface IIntentAnalysisService
{
    Task<UserIntent> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default);
    Task<ConfidenceLevel> ValidateIntentAsync(UserIntent intent, CancellationToken cancellationToken = default);
}
