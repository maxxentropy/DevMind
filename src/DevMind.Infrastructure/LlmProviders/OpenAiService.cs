using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using DevMind.Infrastructure.LlmProviders;
using DevMind.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Infrastructure.LlmProviders;

/// <summary>
/// OpenAI implementation of the LLM service using the Response pattern
/// </summary>
public class OpenAiService : BaseLlmService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAiOptions> _options;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region Properties

    protected override string ProviderName => "openai";

    #endregion

    #region Constructor

    public OpenAiService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiService> logger,
        LlmErrorHandler errorHandler)
        : base(logger, errorHandler)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        ConfigureHttpClient();
    }

    #endregion

    #region Protected Implementation Methods

    protected override async Task<UserIntent> AnalyzeIntentInternalAsync(UserRequest request, CancellationToken cancellationToken)
    {
        var configValidation = ValidateConfiguration();
        if (configValidation.IsFailure)
        {
            throw new InvalidOperationException($"OpenAI configuration is invalid: {configValidation.Error.Message}");
        }

        var intentPrompt = CreateIntentAnalysisPrompt(request.Content);
        var response = await GenerateResponseInternalAsync(intentPrompt, LlmOptions.ForAnalysis, cancellationToken);

        // Parse the response to extract intent type and confidence
        var (intentType, confidence) = ParseIntentResponse(response);

        // Create intent without SessionId for now (maintaining current domain model)
        var intent = UserIntent.Create(request.Content, intentType);
        intent.UpdateConfidence(confidence);

        return intent;
    }

    protected override async Task<ExecutionPlan> CreateExecutionPlanInternalAsync(
        UserIntent intent,
        IEnumerable<DomainToolDefinition> availableTools,
        CancellationToken cancellationToken)
    {
        var planningPrompt = CreatePlanningPrompt(intent, availableTools);
        var response = await GenerateResponseInternalAsync(planningPrompt, LlmOptions.ForAnalysis, cancellationToken);

        var plan = ExecutionPlan.Create(intent);

        // Parse the response to extract tool calls
        // Use null for SessionId since UserIntent doesn't have it
        // The orchestration service will set the proper SessionId when executing
        var toolCalls = ParsePlanningResponse(response, sessionId: null);
        foreach (var toolCall in toolCalls)
        {
            plan.AddStep(toolCall);
        }

        return plan;
    }

    protected override async Task<string> SynthesizeResponseInternalAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolExecution> results,
        CancellationToken cancellationToken)
    {
        var synthesisPrompt = CreateSynthesisPrompt(intent, plan, results);
        return await GenerateResponseInternalAsync(synthesisPrompt, LlmOptions.ForSynthesis, cancellationToken);
    }

    protected override async Task<string> GenerateResponseInternalAsync(
        string prompt,
        LlmOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var chatRequest = CreateChatRequest(prompt, options);
            var requestJson = JsonSerializer.Serialize(chatRequest, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending OpenAI request: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}",
                chatRequest.Model, chatRequest.MaxTokens, chatRequest.Temperature);

            var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent, _jsonOptions);

            if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
            {
                throw new InvalidOperationException("OpenAI API returned no choices in response");
            }

            var message = chatResponse.Choices[0].Message;
            if (string.IsNullOrEmpty(message?.Content))
            {
                throw new InvalidOperationException("OpenAI API returned empty message content");
            }

            // Log usage statistics if available
            if (chatResponse.Usage != null)
            {
                _logger.LogDebug("OpenAI Usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                    chatResponse.Usage.PromptTokens, chatResponse.Usage.CompletionTokens, chatResponse.Usage.TotalTokens);
            }

            return message.Content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during OpenAI API call");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error during OpenAI API call");
            throw new InvalidOperationException("Failed to process OpenAI API response", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during OpenAI API call");
            throw new TimeoutException("OpenAI API request timed out", ex);
        }
    }

    protected override async Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Simple health check using a minimal request
            var healthCheckPrompt = "Hello";
            var response = await GenerateResponseInternalAsync(
                healthCheckPrompt,
                new LlmOptions { MaxTokens = 10, Temperature = 0.1 },
                cancellationToken);

            return !string.IsNullOrWhiteSpace(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI health check failed");
            return false;
        }
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

        // Set base address
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.BaseUrl);
        }

        // Set authentication header
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

        // Set organization header if provided
        if (!string.IsNullOrWhiteSpace(options.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", options.OrganizationId);
        }

        // Set project header if provided
        if (!string.IsNullOrWhiteSpace(options.ProjectId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Project", options.ProjectId);
        }

        // Set user agent
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevMind/1.0");

        // Set timeout
        var timeoutSeconds = options.TimeoutSeconds ?? 30;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    private OpenAiChatRequest CreateChatRequest(string prompt, LlmOptions options)
    {
        var openAiOptions = _options.Value;

        return new OpenAiChatRequest
        {
            Model = openAiOptions.Model,
            Messages = new List<ExternalLlmMessage>
            {
                new ExternalLlmMessage
                {
                    Role = "user",
                    Content = prompt
                }
            },
            MaxTokens = options.MaxTokens,
            Temperature = options.Temperature,
            TopP = options.TopP,
            FrequencyPenalty = openAiOptions.FrequencyPenalty,
            PresencePenalty = openAiOptions.PresencePenalty,
            Stop = options.StopSequences?.Length > 0 ? options.StopSequences : null,
            NumberOfChoices = 1,
            User = openAiOptions.UserId
        };
    }

    private string CreateIntentAnalysisPrompt(string userRequest)
    {
        return $@"Analyze the following user request and determine the most appropriate intent.

User request: ""{userRequest}""

Available intent types:
- AnalyzeCode: Analyze code quality, structure, or issues
- CreateBranch: Create a new git branch
- RunTests: Execute test suites or test cases
- GenerateDocumentation: Create or update documentation
- RefactorCode: Improve code structure or design
- FindBugs: Identify potential bugs or issues
- OptimizePerformance: Improve code performance
- SecurityScan: Analyze code for security vulnerabilities
- Unknown: Cannot determine specific intent

Respond with only the intent type and confidence level (High/Medium/Low) in this format:
Intent: [IntentType]
Confidence: [Level]";
    }

    private string CreatePlanningPrompt(UserIntent intent, IEnumerable<DomainToolDefinition> availableTools)
    {
        var toolsList = string.Join("\n", availableTools.Select(t => $"- {t.Name}: {t.Description}"));

        return $@"Create an execution plan for the following user intent:

Intent: {intent.Type}
Original request: ""{intent.OriginalRequest}""

Available tools:
{toolsList}

Create a step-by-step execution plan using the available tools. For each step, specify:
1. Tool name
2. Parameters to pass to the tool

Respond in this format:
Step 1: [ToolName] with parameters: {{""param1"": ""value1"", ""param2"": ""value2""}}
Step 2: [ToolName] with parameters: {{""param1"": ""value1""}}
...";
    }

    private string CreateSynthesisPrompt(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolExecution> results)
    {
        var resultsText = string.Join("\n", results.Select((r, i) =>
            $"Tool {i + 1} ({r.ToolCall.ToolName}): {r.GetResult<object>() ?? "No result"}"));

        return $@"Synthesize a user-friendly response based on the following execution results:

Original user request: ""{intent.OriginalRequest}""
Intent: {intent.Type}

Execution results:
{resultsText}

Create a comprehensive, helpful response that:
1. Acknowledges what the user requested
2. Summarizes what was accomplished
3. Provides relevant details from the execution results
4. Suggests next steps if appropriate

Keep the tone professional but friendly.";
    }

    private (IntentType intentType, ConfidenceLevel confidence) ParseIntentResponse(string response)
    {
        try
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var intentLine = lines.FirstOrDefault(l => l.StartsWith("Intent:", StringComparison.OrdinalIgnoreCase));
            var confidenceLine = lines.FirstOrDefault(l => l.StartsWith("Confidence:", StringComparison.OrdinalIgnoreCase));

            var intentText = intentLine?.Substring(7).Trim() ?? "Unknown";
            var confidenceText = confidenceLine?.Substring(11).Trim() ?? "Medium";

            var intentType = Enum.TryParse<IntentType>(intentText, true, out var parsedIntent)
                ? parsedIntent
                : IntentType.Unknown;

            var confidence = Enum.TryParse<ConfidenceLevel>(confidenceText, true, out var parsedConfidence)
                ? parsedConfidence
                : ConfidenceLevel.Medium;

            return (intentType, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse intent response, using defaults");
            return (IntentType.Unknown, ConfidenceLevel.Low);
        }
    }

    private List<ToolCall> ParsePlanningResponse(string response, Guid? sessionId)
    {
        var toolCalls = new List<ToolCall>();

        try
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var stepOrder = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("Step", StringComparison.OrdinalIgnoreCase))
                {
                    var toolCall = ParseStepLine(line, sessionId, stepOrder++);
                    if (toolCall != null)
                    {
                        toolCalls.Add(toolCall);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse planning response, returning empty plan");
        }

        return toolCalls;
    }

    private ToolCall? ParseStepLine(string stepLine, Guid? sessionId, int order)
    {
        try
        {
            // Extract tool name and parameters from step line
            // Example: "Step 1: code_analyzer with parameters: {"code": "example", "language": "csharp"}"

            var parts = stepLine.Split(new[] { " with parameters: " }, StringSplitOptions.None);
            if (parts.Length != 2) return null;

            var toolPart = parts[0];
            var paramsPart = parts[1];

            // Extract tool name (after the colon)
            var colonIndex = toolPart.IndexOf(':');
            if (colonIndex == -1) return null;

            var toolName = toolPart.Substring(colonIndex + 1).Trim();

            // Parse parameters JSON
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(paramsPart) && paramsPart.Trim() != "{}")
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(paramsPart, _jsonOptions);
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                        {
                            parameters[kvp.Key] = kvp.Value.GetString() ?? kvp.Value.ToString();
                        }
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Failed to parse parameters JSON: {Params}", paramsPart);
                }
            }

            return ToolCall.Create(toolName, parameters, sessionId, order);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse step line: {StepLine}", stepLine);
            return null;
        }
    }

    #endregion
}
