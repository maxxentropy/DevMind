// src/DevMind.Infrastructure/LlmProviders/BaseLlmService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Infrastructure.LlmProviders;

public abstract class BaseLlmService : ILlmService
{
    protected readonly ILogger _logger;
    protected readonly LlmErrorHandler _errorHandler;

    protected BaseLlmService(ILogger logger, LlmErrorHandler errorHandler)
    {
        _logger = logger;
        _errorHandler = errorHandler;
    }

    protected abstract string ProviderName { get; }

    public async Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => AnalyzeIntentInternalAsync(request, cancellationToken), ProviderName, "AnalyzeIntent", cancellationToken);

    public async Task<Result<ToolCall?>> DetermineNextStepAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, List<Result<ToolExecution>> history, CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => DetermineNextStepInternalAsync(intent, availableTools, history, cancellationToken), ProviderName, "DetermineNextStep", cancellationToken);

    public async Task<Result<string>> SynthesizeResponseAsync(UserIntent intent, IEnumerable<ToolExecution> results, CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => SynthesizeResponseInternalAsync(intent, results, cancellationToken), ProviderName, "SynthesizeResponse", cancellationToken);

    public async Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => GenerateResponseInternalAsync(prompt, options ?? LlmOptions.Default, cancellationToken), ProviderName, "GenerateResponse", cancellationToken);

    public async Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => HealthCheckInternalAsync(cancellationToken), ProviderName, "HealthCheck", cancellationToken);

    public async Task<Result<string>> SummarizeHistoryAsync(UserIntent intent, List<Result<ToolExecution>> history, CancellationToken cancellationToken = default)
        => await _errorHandler.ExecuteWithErrorHandling(() => SummarizeHistoryInternalAsync(intent, history, cancellationToken), ProviderName, "SummarizeHistory", cancellationToken);

    protected abstract Task<UserIntent> AnalyzeIntentInternalAsync(UserRequest request, CancellationToken cancellationToken);
    protected abstract Task<ToolCall?> DetermineNextStepInternalAsync(UserIntent intent, IEnumerable<DomainToolDefinition> availableTools, List<Result<ToolExecution>> history, CancellationToken cancellationToken);
    protected abstract Task<string> SynthesizeResponseInternalAsync(UserIntent intent, IEnumerable<ToolExecution> results, CancellationToken cancellationToken);
    protected abstract Task<string> GenerateResponseInternalAsync(string prompt, LlmOptions options, CancellationToken cancellationToken);
    protected abstract Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken);
    protected abstract Task<string> SummarizeHistoryInternalAsync(UserIntent intent, List<Result<ToolExecution>> history, CancellationToken cancellationToken);

    protected virtual Result ValidateConfiguration() => Result.Success();
}
