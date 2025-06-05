// src/DevMind.Core/Application/Interfaces/ILlmService.cs

using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Interface for Large Language Model services using the Response pattern.
/// All methods return Result types to provide comprehensive error handling.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Analyzes user intent from a request using the LLM
    /// </summary>
    /// <param name="request">The user request to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the analyzed user intent or error information</returns>
    Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an execution plan based on user intent and available tools
    /// </summary>
    /// <param name="intent">The user intent to create a plan for</param>
    /// <param name="availableTools">Tools available for execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the execution plan or error information</returns>
    Task<Result<ExecutionPlan>> CreateExecutionPlanAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes a final response based on intent, plan, and execution results
    /// </summary>
    /// <param name="intent">The original user intent</param>
    /// <param name="plan">The execution plan that was followed</param>
    /// <param name="results">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the synthesized response or error information</returns>
    Task<Result<string>> SynthesizeResponseAsync(UserIntent intent, ExecutionPlan plan, IEnumerable<ToolResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a response for a given prompt using the LLM
    /// </summary>
    /// <param name="prompt">The prompt to generate a response for</param>
    /// <param name="options">Optional LLM generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the generated response or error information</returns>
    Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the LLM service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether the service is healthy</returns>
    Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended interface for LLM services that support streaming responses
/// </summary>
public interface IStreamingLlmService : ILlmService
{
    /// <summary>
    /// Generates a streaming response for a given prompt
    /// </summary>
    /// <param name="prompt">The prompt to generate a response for</param>
    /// <param name="options">Optional LLM generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing an async enumerable of response chunks or error information</returns>
    Task<Result<IAsyncEnumerable<string>>> GenerateStreamingResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended interface for LLM services that support function/tool calling
/// </summary>
public interface IFunctionCallingLlmService : ILlmService
{
    /// <summary>
    /// Generates a response that may include function/tool calls
    /// </summary>
    /// <param name="prompt">The prompt to generate a response for</param>
    /// <param name="availableFunctions">Functions available for the LLM to call</param>
    /// <param name="options">Optional LLM generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the response with potential function calls or error information</returns>
    Task<Result<LlmResponseWithFunctions>> GenerateResponseWithFunctionsAsync(
        string prompt,
        IEnumerable<ToolDefinition> availableFunctions,
        LlmOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from LLM that may include function calls
/// </summary>
public class LlmResponseWithFunctions
{
    /// <summary>
    /// The text response from the LLM
    /// </summary>
    public string TextResponse { get; set; } = string.Empty;

    /// <summary>
    /// Function calls requested by the LLM
    /// </summary>
    public IReadOnlyList<ToolCall> FunctionCalls { get; set; } = Array.Empty<ToolCall>();

    /// <summary>
    /// Whether the response is complete or requires function execution
    /// </summary>
    public bool RequiresFunctionExecution => FunctionCalls.Any();

    /// <summary>
    /// Usage statistics for the request
    /// </summary>
    public LlmUsageStats? Usage { get; set; }
}

/// <summary>
/// Usage statistics for LLM requests
/// </summary>
public class LlmUsageStats
{
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of tokens in the completion
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total number of tokens used
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Estimated cost for the request
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Duration of the request
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// The model used for the request
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The provider used for the request
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}
