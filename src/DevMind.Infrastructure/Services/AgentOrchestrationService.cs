using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Core.Extensions;
using DevMind.Infrastructure.LlmProviders;
using Microsoft.Extensions.Logging;

using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Infrastructure.Services;

/// <summary>
/// Enhanced orchestration service using the Response pattern for comprehensive error handling
/// </summary>
public class AgentOrchestrationService : IAgentOrchestrationService
{
    #region Private Fields

    private readonly ILlmService _llmService;
    private readonly IMcpClientService _mcpClientService;
    private readonly ILogger<AgentOrchestrationService> _logger;

    #endregion

    #region Constructor

    public AgentOrchestrationService(
        ILlmService llmService,
        IMcpClientService mcpClientService,
        ILogger<AgentOrchestrationService> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Processes a user request through the complete AI agent pipeline
    /// </summary>
    /// <param name="request">The user request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the agent response or error information</returns>
    public async Task<Result<AgentResponse>> ProcessUserRequestAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing user request: {Request}", request.Content);

        try
        {
            // Step 1: Analyze user intent
            var intentResult = await AnalyzeUserIntent(request, cancellationToken);
            if (intentResult.IsFailure)
            {
                return CreateErrorResponse(intentResult.Error, "Failed to analyze user intent");
            }

            var intent = intentResult.Value;
            _logger.LogDebug("Analyzed intent: {IntentType} with confidence {Confidence}",
                intent.Type, intent.Confidence);

            // Step 2: Get available tools
            var toolsResult = await GetAvailableTools(cancellationToken);
            if (toolsResult.IsFailure)
            {
                return CreateErrorResponse(toolsResult.Error, "Failed to retrieve available tools");
            }

            var availableTools = toolsResult.Value;
            _logger.LogDebug("Retrieved {ToolCount} available tools", availableTools.Count());

            // Step 3: Create execution plan
            var planResult = await CreateExecutionPlan(intent, availableTools, cancellationToken);
            if (planResult.IsFailure)
            {
                return CreateErrorResponse(planResult.Error, "Failed to create execution plan");
            }

            var plan = planResult.Value;
            _logger.LogDebug("Created execution plan with {StepCount} steps", plan.Steps.Count);

            // Step 4: Execute the plan
            var executionResult = await ExecutePlan(plan, cancellationToken);
            if (executionResult.IsFailure)
            {
                return CreateErrorResponse(executionResult.Error, "Failed to execute plan");
            }

            var toolExecutions = executionResult.Value;
            _logger.LogDebug("Executed plan with {ResultCount} tool results", toolExecutions.Count());

            // Step 5: Synthesize final response
            var responseResult = await SynthesizeResponse(intent, plan, toolExecutions, cancellationToken);
            if (responseResult.IsFailure)
            {
                return CreateErrorResponse(responseResult.Error, "Failed to synthesize response");
            }

            var finalResponse = responseResult.Value;
            _logger.LogInformation("Successfully processed user request");

            return Result<AgentResponse>.Success(AgentResponse.CreateSuccess(finalResponse, ResponseType.Success));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("User request processing was cancelled");
            return Result<AgentResponse>.Success(
                AgentResponse.CreateError("Request was cancelled", request.Content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing user request");
            return Result<AgentResponse>.Failure(
                LlmErrorCodes.Unknown,
                "An unexpected error occurred while processing your request",
                new { OriginalRequest = request.Content, ExceptionType = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Retrieves session history for a user
    /// </summary>
    /// <param name="limit">Maximum number of sessions to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing session history or error information</returns>
    public async Task<Result<IEnumerable<AgentSession>>> GetSessionHistoryAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Result<IEnumerable<AgentSession>>.Failure(
                LlmErrorCodes.InvalidRequest,
                "Limit must be greater than 0");
        }

        try
        {
            _logger.LogDebug("Retrieving session history with limit: {Limit}", limit);

            // TODO: Implement actual session retrieval from storage
            await Task.CompletedTask;

            var emptySessions = Enumerable.Empty<AgentSession>();
            return Result<IEnumerable<AgentSession>>.Success(emptySessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history");
            return Result<IEnumerable<AgentSession>>.Failure(
                LlmErrorCodes.Unknown,
                "Failed to retrieve session history");
        }
    }

    /// <summary>
    /// Continues a conversation from a previous session
    /// </summary>
    /// <param name="request">The new user request</param>
    /// <param name="previousSessionId">ID of the previous session to continue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the continued conversation response</returns>
    public async Task<Result<AgentResponse>> ContinueConversationAsync(
        UserRequest request,
        Guid previousSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Continuing conversation from session: {SessionId}", previousSessionId);

        try
        {
            // TODO: Implement conversation continuation with context from previous session
            // For now, just process as a new request
            var result = await ProcessUserRequestAsync(request, cancellationToken);

            return result.Map(response =>
            {
                // Add context indicating this is a continuation
                response.WithMetadata("previous_session_id", previousSessionId);
                return response;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing conversation");
            return Result<AgentResponse>.Failure(
                LlmErrorCodes.Unknown,
                "Failed to continue conversation");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Analyzes user intent using the LLM service
    /// </summary>
    /// <param name="request">User request to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the analyzed intent</returns>
    private async Task<Result<UserIntent>> AnalyzeUserIntent(UserRequest request, CancellationToken cancellationToken)
    {
        var result = await _llmService.AnalyzeIntentAsync(request, cancellationToken);

        return result.OnFailure(error =>
            _logger.LogWarning("Intent analysis failed: {Error}", error.Message));
    }

    /// <summary>
    /// Retrieves available tools from the MCP client
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing available tools</returns>
    private async Task<Result<IEnumerable<DomainToolDefinition>>> GetAvailableTools(CancellationToken cancellationToken)
    {
        try
        {
            var toolsResult = await _mcpClientService.GetAvailableToolsAsync(cancellationToken);

            if (toolsResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve available tools: {Error}", toolsResult.Error.Message);
                return Result<IEnumerable<DomainToolDefinition>>.Failure(toolsResult.Error);
            }

            return Result<IEnumerable<DomainToolDefinition>>.Success(toolsResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available tools");
            return Result<IEnumerable<DomainToolDefinition>>.Failure(
                LlmErrorCodes.ServiceUnavailable,
                "Tool service is currently unavailable");
        }
    }

    /// <summary>
    /// Creates an execution plan using the LLM service
    /// </summary>
    /// <param name="intent">User intent</param>
    /// <param name="availableTools">Available tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the execution plan</returns>
    private async Task<Result<ExecutionPlan>> CreateExecutionPlan(
        UserIntent intent,
        IEnumerable<DomainToolDefinition> availableTools,
        CancellationToken cancellationToken)
    {
        var result = await _llmService.CreateExecutionPlanAsync(intent, availableTools, cancellationToken);

        return result.OnFailure(error =>
            _logger.LogWarning("Execution plan creation failed: {Error}", error.Message));
    }

    /// <summary>
    /// Executes the plan by running tool calls
    /// </summary>
    /// <param name="plan">Execution plan to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing tool execution results</returns>
    private async Task<Result<IEnumerable<ToolExecution>>> ExecutePlan(
        ExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        try
        {
            plan.StartExecution();

            var results = new List<Result<ToolExecution>>();

            foreach (var step in plan.Steps)
            {
                _logger.LogDebug("Executing tool: {ToolName}", step.ToolName);

                var toolExecutionResult = await _mcpClientService.ExecuteToolAsync(step, cancellationToken);
                results.Add(toolExecutionResult);

                if (toolExecutionResult.IsFailure)
                {
                    _logger.LogWarning("Tool execution failed: {ToolName} - {Error}",
                        step.ToolName, toolExecutionResult.Error.Message);

                    plan.FailExecution($"Tool {step.ToolName} failed: {toolExecutionResult.Error.Message}");
                    break;
                }
            }

            if (plan.Status != ExecutionStatus.Failed)
            {
                plan.CompleteExecution();
            }

            // Extract successful executions for return
            var successfulExecutions = results.GetSuccessful();

            return Result<IEnumerable<ToolExecution>>.Success(successfulExecutions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing plan");
            plan.FailExecution($"Execution error: {ex.Message}");

            return Result<IEnumerable<ToolExecution>>.Failure(
                LlmErrorCodes.ServiceUnavailable,
                "Failed to execute plan due to tool service error");
        }
    }

    /// <summary>
    /// Synthesizes the final response using the LLM service
    /// </summary>
    /// <param name="intent">Original user intent</param>
    /// <param name="plan">Execution plan</param>
    /// <param name="toolExecutions">Results from tool execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the synthesized response</returns>
    private async Task<Result<string>> SynthesizeResponse(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolExecution> toolExecutions,
        CancellationToken cancellationToken)
    {
        var result = await _llmService.SynthesizeResponseAsync(intent, plan, toolExecutions, cancellationToken);

        return result.OnFailure(error =>
            _logger.LogWarning("Response synthesis failed: {Error}", error.Message));
    }

    /// <summary>
    /// Creates an error response from a result error
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="context">Additional context about the error</param>
    /// <returns>Result containing an error agent response</returns>
    private static Result<AgentResponse> CreateErrorResponse(ResultError error, string context)
    {
        var userMessage = error.Code switch
        {
            LlmErrorCodes.Authentication => "I'm having trouble accessing the AI service. Please check the configuration.",
            LlmErrorCodes.RateLimit => "I'm experiencing high demand right now. Please try again in a moment.",
            LlmErrorCodes.ServiceUnavailable => "The AI service is temporarily unavailable. Please try again later.",
            LlmErrorCodes.Timeout => "The request took too long to process. Please try a simpler request.",
            LlmErrorCodes.InvalidRequest => "I couldn't understand your request. Please try rephrasing it.",
            _ => "I encountered an unexpected issue. Please try again or contact support."
        };

        var agentResponse = AgentResponse.CreateError(userMessage)
            .WithMetadata("error_code", error.Code)
            .WithMetadata("error_context", context);

        return Result<AgentResponse>.Success(agentResponse);
    }

    #endregion
}
