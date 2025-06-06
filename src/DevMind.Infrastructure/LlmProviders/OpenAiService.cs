// src/DevMind.Infrastructure/LlmProviders/OpenAiService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using DevMind.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Infrastructure.LlmProviders;

public class OpenAiService : BaseLlmService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAiOptions> _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IPromptService _promptService;

    #endregion

    #region Properties

    protected override string ProviderName => "openai";

    #endregion

    #region Constructor

    public OpenAiService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiService> logger,
        LlmErrorHandler errorHandler,
        IPromptService promptService)
        : base(logger, errorHandler)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    #endregion

    #region Protected Implementation Methods

    protected override async Task<UserIntent> AnalyzeIntentInternalAsync(UserRequest request, CancellationToken cancellationToken)
    {
        var intentPrompt = CreateIntentAnalysisPrompt(request.Content);
        var response = await GenerateResponseInternalAsync(intentPrompt, LlmOptions.ForAnalysis, cancellationToken);
        var (intentType, confidence) = ParseIntentResponse(response);
        var intent = UserIntent.Create(request.Content, intentType, sessionId: request.SessionId);
        intent.UpdateConfidence(confidence);
        return intent;
    }

    protected override async Task<ToolCall?> DetermineNextStepInternalAsync(UserIntent intent, IEnumerable<DomainToolDefinition> availableTools, List<Result<ToolExecution>> history, CancellationToken cancellationToken)
    {
        var planningPrompt = await _promptService.CreateNextStepPromptAsync(intent, availableTools, history);
        var response = await GenerateResponseInternalAsync(planningPrompt, LlmOptions.ForAnalysis, cancellationToken);

        if (string.IsNullOrWhiteSpace(response) || response.Contains("Final Answer:", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Model determined the final answer has been reached or returned no action.");
            return null; // This is the explicit "task complete" signal.
        }

        var toolCall = ParsePlanningResponse(response, intent.SessionId).FirstOrDefault();

        // THIS IS THE FIX: If we did not get a "Final Answer" and we also could not parse a tool call,
        // it's a reasoning failure. We throw an exception, which our LlmErrorHandler will catch
        // and convert into a proper Result.Failure, stopping the orchestration loop correctly.
        if (toolCall == null)
        {
            throw new InvalidOperationException($"The LLM failed to determine the next action. It did not return a valid tool call or the 'Final Answer:' signal. Response was: {response}");
        }

        return toolCall;
    }

    protected override async Task<string> SynthesizeResponseInternalAsync(UserIntent intent, IEnumerable<ToolExecution> results, CancellationToken cancellationToken)
    {
        var synthesisPrompt = await _promptService.CreateSynthesisPromptAsync(intent, results);
        return await GenerateResponseInternalAsync(synthesisPrompt, LlmOptions.ForSynthesis, cancellationToken);
    }

    protected override async Task<string> SummarizeHistoryInternalAsync(UserIntent intent, List<Result<ToolExecution>> history, CancellationToken cancellationToken)
    {
        var summarizationPrompt = CreateSummarizationPrompt(intent, history);
        var options = new LlmOptions { MaxTokens = 500, Temperature = 0.0 };
        return await GenerateResponseInternalAsync(summarizationPrompt, options, cancellationToken);
    }

    protected override async Task<string> GenerateResponseInternalAsync(string prompt, LlmOptions options, CancellationToken cancellationToken)
    {
        var chatRequest = CreateChatRequest(prompt, options);
        var requestJson = JsonSerializer.Serialize(chatRequest, _jsonOptions);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"OpenAI API request failed: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent, _jsonOptions);
        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    protected override async Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken)
    {
        var response = await GenerateResponseInternalAsync("Health check", new LlmOptions { MaxTokens = 5 }, cancellationToken);
        return !string.IsNullOrWhiteSpace(response);
    }

    protected override Result ValidateConfiguration()
    {
        var options = _options.Value;
        var errors = options.Validate();
        return errors.Any()
            ? Result.Failure(LlmErrorCodes.Configuration, $"OpenAI configuration errors: {string.Join(", ", errors)}")
            : Result.Success();
    }

    #endregion

    #region Private Helper Methods

    private void ConfigureHttpClient()
    {
        var options = _options.Value;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl)) _httpClient.BaseAddress = new Uri(options.BaseUrl);
        if (!string.IsNullOrWhiteSpace(options.ApiKey)) _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    private OpenAiChatRequest CreateChatRequest(string prompt, LlmOptions options)
    {
        return new OpenAiChatRequest
        {
            Model = _options.Value.Model,
            Messages = new List<ExternalLlmMessage> { new() { Role = "user", Content = prompt } },
            MaxTokens = options.MaxTokens,
            Temperature = options.Temperature,
            TopP = options.TopP
        };
    }

    private string CreateIntentAnalysisPrompt(string userRequest)
    {
        return $@"Analyze the following user request and determine the most appropriate intent.
User request: ""{userRequest}""
Available intent types: AnalyzeCode, CreateBranch, RunTests, GenerateDocumentation, RefactorCode, FindBugs, OptimizePerformance, SecurityScan, Unknown
Respond with only the intent type and confidence level (High/Medium/Low) in this format:
Intent: [IntentType]
Confidence: [Level]";
    }

    private string CreateSummarizationPrompt(UserIntent intent, List<Result<ToolExecution>> history)
    {
        var historyLog = new StringBuilder();
        foreach (var result in history)
        {
            historyLog.AppendLine(result.IsSuccess
                ? $"- Tool `{result.Value.ToolCall.ToolName}` was called and succeeded."
                : $"- A tool call failed with error: {result.Error.Message}");
        }

        return $@"Based on the initial user request and the following execution history, create a concise, one-sentence summary for the agent's long-term memory.
Focus on the final outcome or key finding.

Initial Request: ""{intent.OriginalRequest}""

Execution History:
{historyLog}

Concise Summary:";
    }

    private (IntentType, ConfidenceLevel) ParseIntentResponse(string response)
    {
        try
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var intentLine = lines.FirstOrDefault(l => l.StartsWith("Intent:", StringComparison.OrdinalIgnoreCase));
            var confidenceLine = lines.FirstOrDefault(l => l.StartsWith("Confidence:", StringComparison.OrdinalIgnoreCase));
            var intentText = intentLine?.Substring("Intent:".Length).Trim() ?? "Unknown";
            var confidenceText = confidenceLine?.Substring("Confidence:".Length).Trim() ?? "Medium";
            Enum.TryParse<IntentType>(intentText, true, out var parsedIntent);
            Enum.TryParse<ConfidenceLevel>(confidenceText, true, out var parsedConfidence);
            return (parsedIntent, parsedConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse intent response, using defaults.");
            return (IntentType.Unknown, ConfidenceLevel.Low);
        }
    }

    private List<ToolCall> ParsePlanningResponse(string response, Guid? sessionId)
    {
        var toolCalls = new List<ToolCall>();
        try
        {
            if (response.Trim().StartsWith("{"))
            {
                var toolCall = ParseToolCallJson(response, sessionId);
                if (toolCall != null) toolCalls.Add(toolCall);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse planning response. Response was: {Response}", response);
        }
        return toolCalls;
    }

    private ToolCall? ParseToolCallJson(string json, Guid? sessionId)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);

            // Flexibly get the tool name, preferring "name" but accepting "tool".
            var nameElement = parsed.TryGetProperty("name", out var n) ? n
                            : parsed.TryGetProperty("tool", out var t) ? t
                            : default;

            // Flexibly get the arguments, preferring "arguments" but accepting "parameters".
            var argsElement = parsed.TryGetProperty("arguments", out var a) ? a
                            : parsed.TryGetProperty("parameters", out var p) ? p
                            : default;

            if (nameElement.ValueKind != JsonValueKind.Undefined && argsElement.ValueKind != JsonValueKind.Undefined)
            {
                var toolName = nameElement.GetString()!;
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText(), options)!;
                return ToolCall.Create(toolName, parameters, sessionId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse response as a direct tool call JSON object.");
        }
        return null;
    }
    #endregion
}
