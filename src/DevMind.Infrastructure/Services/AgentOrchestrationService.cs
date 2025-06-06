using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.LlmProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var intentResult = await _llmService.AnalyzeIntentAsync(request, cancellationToken);
            if (intentResult.IsFailure) return CreateErrorResponse(intentResult.Error, "Failed to analyze user intent.");
            var intent = intentResult.Value;

            var availableTools = (await _mcpClientService.GetAvailableToolsAsync(cancellationToken)).Value ?? Enumerable.Empty<ToolDefinition>();
            var maxIterations = 10;
            for (int i = 0; i < maxIterations; i++)
            {
                var nextStepResult = await _llmService.DetermineNextStepAsync(intent, availableTools, shortTermHistory, cancellationToken);
                if (nextStepResult.IsFailure) return CreateErrorResponse(nextStepResult.Error, "Could not determine next step.");

                var toolCall = nextStepResult.Value;
                if (toolCall == null) break;

                var guardrailCheck = await _guardrailService.IsActionAllowedAsync(toolCall);
                var toolExecutionResult = guardrailCheck.IsSuccess
                    ? await _mcpClientService.ExecuteToolAsync(toolCall, cancellationToken)
                    : ToolExecution.Failure(toolCall, guardrailCheck.Error.Code, guardrailCheck.Error.Message);

                shortTermHistory.Add(toolExecutionResult);
            }

            var successfulExecutions = shortTermHistory.Where(r => r.IsSuccess).Select(r => r.Value);

            // CORRECTED: The orchestrator tells the LLM service to synthesize the response.
            // It does not know or care about how the prompt is created.
            var synthesisResult = await _llmService.SynthesizeResponseAsync(intent, successfulExecutions, cancellationToken);

            if (synthesisResult.IsFailure) return CreateErrorResponse(synthesisResult.Error, "Failed to synthesize final response.");

            var validatedOutputResult = await _guardrailService.ValidateOutputAsync(synthesisResult.Value);
            if (validatedOutputResult.IsFailure) return CreateErrorResponse(validatedOutputResult.Error, "Final response failed validation.");

            await _longTermMemoryService.SaveHistoryAsync(sessionId, shortTermHistory);

            var agentResponse = AgentResponse.CreateSuccess(validatedOutputResult.Value).WithMetadata("sessionId", sessionId);
            return Result<AgentResponse>.Success(agentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catastrophic failure in orchestration loop for Session ID: {SessionId}", sessionId);
            return Result<AgentResponse>.Failure(LlmErrorCodes.Unknown, "An unexpected error occurred during processing.");
        }
    }

    // ... Other methods ...
    public Task<Result<IEnumerable<AgentSession>>> GetSessionHistoryAsync(int limit = 10, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Result<AgentResponse>> ContinueConversationAsync(UserRequest request, Guid previousSessionId, CancellationToken cancellationToken = default) => ProcessUserRequestAsync(request with { SessionId = previousSessionId }, cancellationToken);
    private static Result<AgentResponse> CreateErrorResponse(ResultError error, string context) => Result<AgentResponse>.Success(AgentResponse.CreateError(error.Message, context).WithMetadata("error_code", error.Code));
}
