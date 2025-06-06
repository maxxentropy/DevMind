// src/DevMind.Infrastructure/LlmProviders/AnthropicService.cs

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
/// Anthropic Claude implementation of the LLM service using the Response pattern
/// </summary>
public class AnthropicService : BaseLlmService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<AnthropicOptions> _options;
    private readonly IPromptService _promptService;

    #endregion

    #region Properties

    protected override string ProviderName => "anthropic";

    #endregion

    #region Constructor

    public AnthropicService(
        HttpClient httpClient,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicService> logger,
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
        // A real implementation would use a specific prompt for Anthropic
        await Task.Delay(150, cancellationToken); // Simulate API call
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
        // A real implementation would use a specific summarization prompt.
        await Task.Delay(200, cancellationToken);
        return $"Anthropic summary for intent '{intent.OriginalRequest}'.";
    }

    protected override async Task<string> GenerateResponseInternalAsync(
        string prompt,
        LlmOptions options,
        CancellationToken cancellationToken)
    {
        // This is a placeholder for the actual Anthropic API call.
        // A real implementation would construct a JSON body for the Anthropic Messages API.
        await Task.Delay(450, cancellationToken); // Simulate API call
        _logger.LogDebug("Simulating Anthropic API call.");

        // Simulate a plausible response for testing the reasoning loop
        if (prompt.Contains("Next Action:"))
        {
            return "{ \"name\": \"list_plugins\", \"arguments\": {} }";
        }

        return $"Claude response for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }

    protected override async Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(60, cancellationToken); // Simulate health check
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override Result ValidateConfiguration()
    {
        var options = _options.Value;
        var errors = options.Validate();

        return errors.Any()
            ? Result.Failure(LlmErrorCodes.Configuration, $"Anthropic configuration errors: {string.Join(", ", errors)}")
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
            _logger.LogWarning(ex, "Could not parse response from Anthropic as a tool call JSON object. Response: {Response}", json);
        }
        return null;
    }

    #endregion
}
