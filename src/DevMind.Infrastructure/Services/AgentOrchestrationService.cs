using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.LlmProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevMind.Infrastructure.Services;

public class AgentOrchestrationService : IAgentOrchestrationService
{
    private readonly ILlmService _llmService;
    private readonly IMcpClientService _mcpClientService;
    private readonly IGuardrailService _guardrailService;
    private readonly ILongTermMemoryService _longTermMemoryService;
    private readonly ILogger<AgentOrchestrationService> _logger;

    public AgentOrchestrationService(
        ILlmService llmService,
        IMcpClientService mcpClientService,
        IGuardrailService guardrailService,
        ILongTermMemoryService longTermMemoryService,
        ILogger<AgentOrchestrationService> logger)
    {
        _llmService = llmService;
        _mcpClientService = mcpClientService;
        _guardrailService = guardrailService;
        _longTermMemoryService = longTermMemoryService;
        _logger = logger;
    }

    public async Task<Result<AgentResponse>> ProcessUserRequestAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid();
        _logger.LogInformation("Processing request for Session ID: {SessionId}", sessionId);

        try
        {
            var validatedInputResult = await _guardrailService.ValidateInputAsync(request.Content);
            if (validatedInputResult.IsFailure) return CreateErrorResponse(validatedInputResult.Error, "Input failed validation.");

            var shortTermHistory = await _longTermMemoryService.LoadHistoryAsync(sessionId);

            _logger.LogInformation("Determining user intent...");
            var intentResult = await _llmService.AnalyzeIntentAsync(request, cancellationToken);
            if (intentResult.IsFailure) return CreateErrorResponse(intentResult.Error, "Failed to analyze user intent.");
            var intent = intentResult.Value;
            _logger.LogInformation("Intent determined: {IntentType} with {Confidence} confidence", intent.Type, intent.Confidence);

            var availableToolsResult = await _mcpClientService.GetAvailableToolsAsync(cancellationToken);
            if (availableToolsResult.IsFailure) return CreateErrorResponse(availableToolsResult.Error, "Failed to retrieve available tools.");
            var availableTools = availableToolsResult.Value ?? Enumerable.Empty<ToolDefinition>();
            _logger.LogInformation("Found {ToolCount} available tools.", availableTools.Count());

            var maxIterations = 10;
            for (int i = 0; i < maxIterations; i++)
            {
                _logger.LogInformation("Loop {Iteration}: Determining next step...", i + 1);
                var nextStepResult = await _llmService.DetermineNextStepAsync(intent, availableTools, shortTermHistory, cancellationToken);
                if (nextStepResult.IsFailure) return CreateErrorResponse(nextStepResult.Error, "Could not determine next step.");

                var toolCall = nextStepResult.Value;
                if (toolCall == null)
                {
                    _logger.LogInformation("LLM decided the task is complete. Proceeding to synthesize response.");
                    break;
                }
                _logger.LogInformation("LLM decided to call tool: {ToolName} with arguments: {Arguments}", toolCall.ToolName, JsonSerializer.Serialize(toolCall.Parameters));


                var guardrailCheck = await _guardrailService.IsActionAllowedAsync(toolCall);
                Result<ToolExecution> toolExecutionResult;
                if (guardrailCheck.IsSuccess)
                {
                    _logger.LogInformation("Executing tool: {ToolName}", toolCall.ToolName);
                    toolExecutionResult = await _mcpClientService.ExecuteToolAsync(toolCall, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Guardrail blocked tool execution: {ToolName}. Reason: {Reason}", toolCall.ToolName, guardrailCheck.Error.Message);
                    toolExecutionResult = ToolExecution.Failure(toolCall, guardrailCheck.Error.Code, guardrailCheck.Error.Message);
                }


                shortTermHistory.Add(toolExecutionResult);

                if (toolExecutionResult.IsSuccess)
                {
                    _logger.LogInformation("Tool {ToolName} executed successfully in {Duration}ms.", toolExecutionResult.Value.ToolCall.ToolName, toolExecutionResult.Value.Duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogError("Tool {ToolName} failed. Error: {ErrorCode} - {ErrorMessage}", toolCall.ToolName, toolExecutionResult.Error.Code, toolExecutionResult.Error.Message);
                }

            }

            var successfulExecutions = shortTermHistory.Where(r => r.IsSuccess).Select(r => r.Value);

            _logger.LogInformation("Synthesizing final response...");
            var synthesisResult = await _llmService.SynthesizeResponseAsync(intent, successfulExecutions, cancellationToken);

            if (synthesisResult.IsFailure) return CreateErrorResponse(synthesisResult.Error, "Failed to synthesize final response.");

            var validatedOutputResult = await _guardrailService.ValidateOutputAsync(synthesisResult.Value);
            if (validatedOutputResult.IsFailure) return CreateErrorResponse(validatedOutputResult.Error, "Final response failed validation.");

            await _longTermMemoryService.SaveHistoryAsync(sessionId, shortTermHistory);
            _logger.LogInformation("Saved session history for Session ID: {SessionId}", sessionId);

            var agentResponse = AgentResponse.CreateSuccess(validatedOutputResult.Value)
                                    .WithMetadata("sessionId", sessionId)
                                    .WithMetadata("intent_type", intent.Type)
                                    .WithMetadata("tool_executions", shortTermHistory.Count);
            return Result<AgentResponse>.Success(agentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catastrophic failure in orchestration loop for Session ID: {SessionId}", sessionId);
            return Result<AgentResponse>.Failure(LlmErrorCodes.Unknown, "An unexpected error occurred during processing.");
        }
    }

    public Task<Result<IEnumerable<AgentSession>>> GetSessionHistoryAsync(int limit = 10, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Result<AgentResponse>> ContinueConversationAsync(UserRequest request, Guid previousSessionId, CancellationToken cancellationToken = default) => ProcessUserRequestAsync(request with { SessionId = previousSessionId }, cancellationToken);

    private static Result<AgentResponse> CreateErrorResponse(ResultError error, string context)
    {
        // Use the specific error message and code from the ResultError object
        var response = AgentResponse.CreateError(error.Message, context)
            .WithMetadata("error_code", error.Code)
            .WithMetadata("error_details", error.Details!)
            .WithMetadata("error_timestamp", error.Timestamp);
        return Result<AgentResponse>.Success(response);
    }
}
