// tests/DevMind.TestUtilities/Mocks/MockLlmService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevMind.Infrastructure.LlmProviders;

/// <summary>
/// Mock implementation of ILlmService for testing purposes
/// </summary>
public class MockLlmService : ILlmService
{
    #region Private Fields

    private readonly Queue<Result<UserIntent>> _intentResponses = new();
    private readonly Queue<Result<ToolCall?>> _nextStepResponses = new();
    private readonly Queue<Result<string>> _synthesisResponses = new();
    private readonly Queue<Result<string>> _summarizationResponses = new();
    private readonly Queue<Result<string>> _generationResponses = new();
    private readonly Queue<Result<bool>> _healthCheckResponses = new();

    #endregion

    #region Configuration Methods

    public void SetupIntentAnalysis(Result<UserIntent> response) => _intentResponses.Enqueue(response);
    public void SetupNextStep(Result<ToolCall?> response) => _nextStepResponses.Enqueue(response);
    public void SetupSynthesis(Result<string> response) => _synthesisResponses.Enqueue(response);
    public void SetupSummarization(Result<string> response) => _summarizationResponses.Enqueue(response);
    public void SetupGeneration(Result<string> response) => _generationResponses.Enqueue(response);
    public void SetupHealthCheck(Result<bool> response) => _healthCheckResponses.Enqueue(response);

    #endregion

    #region ILlmService Implementation

    public Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        if (_intentResponses.Any()) return Task.FromResult(_intentResponses.Dequeue());
        var intent = UserIntent.Create(request.Content, IntentType.AnalyzeCode, sessionId: request.SessionId);
        return Task.FromResult(Result<UserIntent>.Success(intent));
    }

    public Task<Result<ToolCall?>> DetermineNextStepAsync(
        UserIntent intent,
        IEnumerable<ToolDefinition> availableTools,
        List<Result<ToolExecution>> history,
        CancellationToken cancellationToken = default)
    {
        if (_nextStepResponses.Any()) return Task.FromResult(_nextStepResponses.Dequeue());

        // Default behavior: Return one tool call, then finish.
        if (!history.Any())
        {
            var toolCall = ToolCall.Create("mock_tool", new Dictionary<string, object> { ["input"] = intent.OriginalRequest }, intent.SessionId);
            return Task.FromResult(Result<ToolCall?>.Success(toolCall));
        }

        return Task.FromResult(Result<ToolCall?>.Success(null)); // End loop
    }

    public Task<Result<string>> SynthesizeResponseAsync(UserIntent intent, IEnumerable<ToolExecution> results, CancellationToken cancellationToken = default)
    {
        if (_synthesisResponses.Any()) return Task.FromResult(_synthesisResponses.Dequeue());
        var response = $"Mock synthesis for intent '{intent.OriginalRequest}' with {results.Count()} tool results.";
        return Task.FromResult(Result<string>.Success(response));
    }

    public Task<Result<string>> SummarizeHistoryAsync(UserIntent intent, List<Result<ToolExecution>> history, CancellationToken cancellationToken = default)
    {
        if (_summarizationResponses.Any()) return Task.FromResult(_summarizationResponses.Dequeue());
        var response = $"Mock summary of {history.Count} events for intent '{intent.OriginalRequest}'.";
        return Task.FromResult(Result<string>.Success(response));
    }

    public Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_generationResponses.Any()) return Task.FromResult(_generationResponses.Dequeue());
        var response = $"Mock generated response for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
        return Task.FromResult(Result<string>.Success(response));
    }

    public Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (_healthCheckResponses.Any()) return Task.FromResult(_healthCheckResponses.Dequeue());
        return Task.FromResult(Result<bool>.Success(true));
    }

    #endregion

    #region Helper Methods

    public void Reset()
    {
        _intentResponses.Clear();
        _nextStepResponses.Clear();
        _synthesisResponses.Clear();
        _summarizationResponses.Clear();
        _generationResponses.Clear();
        _healthCheckResponses.Clear();
    }

    public void SetupSuccessfulWorkflow()
    {
        Reset();
        var intent = UserIntent.Create("test request", IntentType.AnalyzeCode);
        SetupIntentAnalysis(Result<UserIntent>.Success(intent));
        SetupNextStep(Result<ToolCall?>.Success(ToolCall.Create("test_tool")));
        SetupNextStep(Result<ToolCall?>.Success(null)); // End loop
        SetupSynthesis(Result<string>.Success("Test synthesis response"));
        SetupSummarization(Result<string>.Success("Test summary response"));
        SetupGeneration(Result<string>.Success("Test generation response"));
        SetupHealthCheck(Result<bool>.Success(true));
    }

    #endregion
}
