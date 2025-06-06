// src/DevMind.Core/Application/Interfaces/ILlmService.cs

using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DomainToolDefinition = DevMind.Core.Domain.ValueObjects.ToolDefinition;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Interface for Large Language Model services using the Response pattern.
/// All methods return Result types to provide comprehensive error handling.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Analyzes user intent from a request using the LLM.
    /// </summary>
    Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the next single tool call to execute based on the current state.
    /// </summary>
    Task<Result<ToolCall?>> DetermineNextStepAsync(
        UserIntent intent,
        IEnumerable<ToolDefinition> availableTools,
        List<Result<ToolExecution>> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes a final response based on intent and execution results.
    /// </summary>
    Task<Result<string>> SynthesizeResponseAsync(
        UserIntent intent,
        IEnumerable<ToolExecution> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarizes a completed execution history into a concise memory for long-term storage.
    /// </summary>
    Task<Result<string>> SummarizeHistoryAsync(
        UserIntent intent,
        List<Result<ToolExecution>> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a response for a given prompt using the LLM.
    /// </summary>
    Task<Result<string>> GenerateResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the LLM service.
    /// </summary>
    Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended interface for LLM services that support streaming responses.
/// </summary>
public interface IStreamingLlmService : ILlmService
{
    /// <summary>
    /// Generates a streaming response for a given prompt.
    /// </summary>
    Task<Result<IAsyncEnumerable<string>>> GenerateStreamingResponseAsync(string prompt, LlmOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended interface for LLM services that support function/tool calling.
/// </summary>
public interface IFunctionCallingLlmService : ILlmService
{
    /// <summary>
    /// Generates a response that may include function/tool calls.
    /// </summary>
    Task<Result<LlmResponseWithFunctions>> GenerateResponseWithFunctionsAsync(
        string prompt,
        IEnumerable<ToolDefinition> availableFunctions,
        LlmOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from LLM that may include function calls.
/// </summary>
public class LlmResponseWithFunctions
{
    public string TextResponse { get; set; } = string.Empty;
    public IReadOnlyList<ToolCall> FunctionCalls { get; set; } = Array.Empty<ToolCall>();
    public bool RequiresFunctionExecution => FunctionCalls.Any();
    public LlmUsageStats? Usage { get; set; }
}

/// <summary>
/// Usage statistics for LLM requests.
/// </summary>
public class LlmUsageStats
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal EstimatedCost { get; set; }
    public TimeSpan Duration { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}
