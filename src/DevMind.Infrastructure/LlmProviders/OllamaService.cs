// src/DevMind.Infrastructure/LlmProviders/OllamaService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Infrastructure.LlmProviders;

/// <summary>
/// Ollama implementation of the LLM service using the Response pattern
/// </summary>
public class OllamaService : BaseLlmService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<OllamaOptions> _options;
    private readonly IPromptService _promptService;

    #endregion

    #region Properties

    protected override string ProviderName => "ollama";

    #endregion

    #region Constructor

    public OllamaService(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaService> logger,
        LlmErrorHandler errorHandler,
        IPromptService promptService) // Inject the new service
        : base(logger, errorHandler)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    #endregion

    #region Protected Implementation Methods

    protected override async Task<UserIntent> AnalyzeIntentInternalAsync(UserRequest request, CancellationToken cancellationToken)
    {
        var configValidation = ValidateConfiguration();
        if (configValidation.IsFailure)
        {
            throw new InvalidOperationException($"Ollama configuration is invalid: {configValidation.Error.Message}");
        }

        await Task.Delay(200, cancellationToken); // Simulate API call
        return UserIntent.Create(request.Content, IntentType.AnalyzeCode, sessionId: request.SessionId);
    }

    protected override async Task<ToolCall?> DetermineNextStepInternalAsync(
        UserIntent intent,
        IEnumerable<DomainToolDefinition> availableTools,
        List<Result<ToolExecution>> history,
        CancellationToken cancellationToken)
    {
        var planningPrompt = await _promptService.CreateNextStepPromptAsync(intent, availableTools, history);
        var response = await GenerateResponseInternalAsync(planningPrompt, LlmOptions.ForAnalysis, cancellationToken);

        if (string.IsNullOrWhiteSpace(response) || response.Contains("Final Answer:", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Task is complete
        }

        return ParseToolCallJson(response, intent.SessionId);
    }

    protected override async Task<string> SynthesizeResponseInternalAsync(
        UserIntent intent,
        IEnumerable<ToolExecution> results,
        CancellationToken cancellationToken)
    {
        var synthesisPrompt = await _promptService.CreateSynthesisPromptAsync(intent, results);
        return await GenerateResponseInternalAsync(synthesisPrompt, LlmOptions.ForSynthesis, cancellationToken);
    }

    protected override async Task<string> SummarizeHistoryInternalAsync(UserIntent intent, List<Result<ToolExecution>> history, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simulate API call
        return $"Ollama summary for intent '{intent.OriginalRequest}'.";
    }


    protected override async Task<string> GenerateResponseInternalAsync(
        string prompt,
        LlmOptions options,
        CancellationToken cancellationToken)
    {
        // This would be replaced with the actual HTTP call to the Ollama /api/generate or /api/chat endpoint
        await Task.Delay(600, cancellationToken); // Simulate API call
        _logger.LogDebug("Simulating Ollama API call.");

        // Simulate a plausible response for testing the reasoning loop
        if (prompt.Contains("Next Action:"))
        {
            return "{ \"name\": \"code_analyzer\", \"arguments\": { \"language\": \"csharp\" } }";
        }

        return $"Ollama local response for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }

    protected override async Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            // A real implementation would make a GET request to the Ollama server's base URL
            var response = await _httpClient.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed.");
            return false;
        }
    }

    protected override Result ValidateConfiguration()
    {
        var options = _options.Value;
        var errors = options.Validate();

        return errors.Any()
            ? Result.Failure(LlmErrorCodes.Configuration, $"Ollama configuration errors: {string.Join(", ", errors)}")
            : Result.Success();
    }

    #endregion

    #region Private Helper Methods

    private ToolCall? ParseToolCallJson(string json, Guid? sessionId)
    {
        try
        {
            var jsonStartIndex = json.IndexOf('{');
            var jsonEndIndex = json.LastIndexOf('}');
            if (jsonStartIndex == -1 || jsonEndIndex == -1) return null;

            var cleanJson = json.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<JsonElement>(cleanJson);

            if (parsed.TryGetProperty("name", out var nameElement) &&
                parsed.TryGetProperty("arguments", out var argsElement))
            {
                var toolName = nameElement.GetString()!;
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText(), options)!;
                return ToolCall.Create(toolName, parameters, sessionId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse response from Ollama as a tool call JSON object. Response: {Response}", json);
        }
        return null;
    }

    #endregion
}
