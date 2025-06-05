// src/DevMind.Shared/Models/ExternalLlmModels.cs

using System.Text.Json.Serialization;

namespace DevMind.Shared.Models;

// <summary>
// External models for LLM API communication across all providers.
// These models represent the wire format for different LLM APIs.
// </summary>

// ==================== COMMON BASE MODELS ====================

/// <summary>
/// Base request model for all LLM providers
/// </summary>
public abstract class ExternalLlmRequestBase
{
    /// <summary>
    /// The model to use for generation
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for response generation (0.0 to 2.0)
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// Base response model for all LLM providers
/// </summary>
public abstract class ExternalLlmResponseBase
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Generated content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information
    /// </summary>
    public ExternalLlmUsage? Usage { get; set; }

    /// <summary>
    /// Additional metadata from the provider
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Token usage information
/// </summary>
public class ExternalLlmUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Estimated cost in USD (calculated by DevMind)
    /// </summary>
    public decimal EstimatedCost { get; set; }
}

/// <summary>
/// Message format used across providers
/// </summary>
public class ExternalLlmMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // system, user, assistant

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("function_call")]
    public ExternalFunctionCall? FunctionCall { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<ExternalToolCall>? ToolCalls { get; set; }
}

// ==================== OPENAI MODELS ====================

/// <summary>
/// OpenAI Chat Completion request model
/// </summary>
public class OpenAiChatRequest : ExternalLlmRequestBase
{
    [JsonPropertyName("messages")]
    public List<ExternalLlmMessage> Messages { get; set; } = new();

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 1.0;

    [JsonPropertyName("frequency_penalty")]
    public double FrequencyPenalty { get; set; } = 0.0;

    [JsonPropertyName("presence_penalty")]
    public double PresencePenalty { get; set; } = 0.0;

    [JsonPropertyName("stop")]
    public string[]? Stop { get; set; }

    [JsonPropertyName("n")]
    public int NumberOfChoices { get; set; } = 1;

    [JsonPropertyName("logit_bias")]
    public Dictionary<string, double>? LogitBias { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("functions")]
    public List<ExternalFunction>? Functions { get; set; }

    [JsonPropertyName("function_call")]
    public object? FunctionCall { get; set; } // string or object

    [JsonPropertyName("tools")]
    public List<ExternalTool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; } // string or object

    [JsonPropertyName("response_format")]
    public ExternalResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("logprobs")]
    public bool? Logprobs { get; set; }

    [JsonPropertyName("top_logprobs")]
    public int? TopLogprobs { get; set; }
}

/// <summary>
/// OpenAI Chat Completion response model
/// </summary>
public class OpenAiChatResponse : ExternalLlmResponseBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public new OpenAiUsage? Usage { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }
}

/// <summary>
/// OpenAI choice object
/// </summary>
public class OpenAiChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ExternalLlmMessage? Message { get; set; }

    [JsonPropertyName("delta")]
    public ExternalLlmMessage? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    public OpenAiLogprobs? Logprobs { get; set; }
}

/// <summary>
/// OpenAI usage object
/// </summary>
public class OpenAiUsage : ExternalLlmUsage
{
    [JsonPropertyName("completion_tokens_details")]
    public OpenAiCompletionTokensDetails? CompletionTokensDetails { get; set; }
}

/// <summary>
/// OpenAI completion tokens details
/// </summary>
public class OpenAiCompletionTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int ReasoningTokens { get; set; }
}

/// <summary>
/// OpenAI logprobs object
/// </summary>
public class OpenAiLogprobs
{
    [JsonPropertyName("content")]
    public List<OpenAiLogprobContent>? Content { get; set; }
}

/// <summary>
/// OpenAI logprob content
/// </summary>
public class OpenAiLogprobContent
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("logprob")]
    public double Logprob { get; set; }

    [JsonPropertyName("bytes")]
    public int[]? Bytes { get; set; }

    [JsonPropertyName("top_logprobs")]
    public List<OpenAiTopLogprob>? TopLogprobs { get; set; }
}

/// <summary>
/// OpenAI top logprob
/// </summary>
public class OpenAiTopLogprob
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("logprob")]
    public double Logprob { get; set; }

    [JsonPropertyName("bytes")]
    public int[]? Bytes { get; set; }
}

// ==================== ANTHROPIC MODELS ====================

/// <summary>
/// Anthropic Messages API request model
/// </summary>
public class AnthropicMessagesRequest : ExternalLlmRequestBase
{
    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 1.0;

    [JsonPropertyName("top_k")]
    public int TopK { get; set; } = 40;

    [JsonPropertyName("stop_sequences")]
    public string[]? StopSequences { get; set; }

    [JsonPropertyName("metadata")]
    public AnthropicMetadata? Metadata { get; set; }

    [JsonPropertyName("tools")]
    public List<AnthropicTool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }
}

/// <summary>
/// Anthropic message model
/// </summary>
public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // user, assistant

    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty; // string or array of content blocks
}

/// <summary>
/// Anthropic content block
/// </summary>
public class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // text, image, tool_use, tool_result

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("source")]
    public AnthropicImageSource? Source { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    public object? Input { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("is_error")]
    public bool? IsError { get; set; }
}

/// <summary>
/// Anthropic image source
/// </summary>
public class AnthropicImageSource
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Anthropic metadata
/// </summary>
public class AnthropicMetadata
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}

/// <summary>
/// Anthropic tool definition
/// </summary>
public class AnthropicTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public object InputSchema { get; set; } = new();
}

/// <summary>
/// Anthropic Messages API response model
/// </summary>
public class AnthropicMessagesResponse : ExternalLlmResponseBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<AnthropicContentBlock> ContentBlocks { get; set; } = new();

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    [JsonPropertyName("usage")]
    public new AnthropicUsage? Usage { get; set; }
}

/// <summary>
/// Anthropic usage object
/// </summary>
public class AnthropicUsage : ExternalLlmUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    // Override base properties to map to Anthropic naming
    [JsonIgnore]
    public new int PromptTokens
    {
        get => InputTokens;
        set => InputTokens = value;
    }

    [JsonIgnore]
    public new int CompletionTokens
    {
        get => OutputTokens;
        set => OutputTokens = value;
    }

    [JsonIgnore]
    public new int TotalTokens => InputTokens + OutputTokens;
}

// ==================== AZURE OPENAI MODELS ====================

/// <summary>
/// Azure OpenAI request (extends OpenAI with Azure-specific fields)
/// </summary>
public class AzureOpenAiChatRequest : OpenAiChatRequest
{
    // Azure OpenAI uses the same format as OpenAI but with different endpoints
    // Additional Azure-specific fields can be added here if needed
}

/// <summary>
/// Azure OpenAI response (extends OpenAI with Azure-specific fields)
/// </summary>
public class AzureOpenAiChatResponse : OpenAiChatResponse
{
    [JsonPropertyName("prompt_filter_results")]
    public List<AzureContentFilterResult>? PromptFilterResults { get; set; }
}

/// <summary>
/// Azure content filter result
/// </summary>
public class AzureContentFilterResult
{
    [JsonPropertyName("content_filter_results")]
    public AzureContentFilterDetails? ContentFilterResults { get; set; }
}

/// <summary>
/// Azure content filter details
/// </summary>
public class AzureContentFilterDetails
{
    [JsonPropertyName("hate")]
    public AzureContentFilterCategory? Hate { get; set; }

    [JsonPropertyName("self_harm")]
    public AzureContentFilterCategory? SelfHarm { get; set; }

    [JsonPropertyName("sexual")]
    public AzureContentFilterCategory? Sexual { get; set; }

    [JsonPropertyName("violence")]
    public AzureContentFilterCategory? Violence { get; set; }
}

/// <summary>
/// Azure content filter category
/// </summary>
public class AzureContentFilterCategory
{
    [JsonPropertyName("filtered")]
    public bool Filtered { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }
}

// ==================== OLLAMA MODELS ====================

/// <summary>
/// Ollama chat request model
/// </summary>
public class OllamaChatRequest : ExternalLlmRequestBase
{
    [JsonPropertyName("messages")]
    public List<ExternalLlmMessage> Messages { get; set; } = new();

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }
}

/// <summary>
/// Ollama generation request model
/// </summary>
public class OllamaGenerateRequest : ExternalLlmRequestBase
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("context")]
    public int[]? Context { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("raw")]
    public bool Raw { get; set; } = false;

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }
}

/// <summary>
/// Ollama options for fine-tuning generation
/// </summary>
public class OllamaOptions
{
    [JsonPropertyName("num_keep")]
    public int? NumKeep { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("tfs_z")]
    public double? TfsZ { get; set; }

    [JsonPropertyName("typical_p")]
    public double? TypicalP { get; set; }

    [JsonPropertyName("repeat_last_n")]
    public int? RepeatLastN { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("repeat_penalty")]
    public double? RepeatPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("mirostat")]
    public int? Mirostat { get; set; }

    [JsonPropertyName("mirostat_tau")]
    public double? MirostatTau { get; set; }

    [JsonPropertyName("mirostat_eta")]
    public double? MirostatEta { get; set; }

    [JsonPropertyName("penalize_newline")]
    public bool? PenalizeNewline { get; set; }

    [JsonPropertyName("stop")]
    public string[]? Stop { get; set; }

    [JsonPropertyName("numa")]
    public bool? Numa { get; set; }

    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    [JsonPropertyName("num_batch")]
    public int? NumBatch { get; set; }

    [JsonPropertyName("num_gqa")]
    public int? NumGqa { get; set; }

    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    [JsonPropertyName("main_gpu")]
    public int? MainGpu { get; set; }

    [JsonPropertyName("low_vram")]
    public bool? LowVram { get; set; }

    [JsonPropertyName("f16_kv")]
    public bool? F16Kv { get; set; }

    [JsonPropertyName("logits_all")]
    public bool? LogitsAll { get; set; }

    [JsonPropertyName("vocab_only")]
    public bool? VocabOnly { get; set; }

    [JsonPropertyName("use_mmap")]
    public bool? UseMmap { get; set; }

    [JsonPropertyName("use_mlock")]
    public bool? UseMlock { get; set; }

    [JsonPropertyName("num_thread")]
    public int? NumThread { get; set; }
}

/// <summary>
/// Ollama response model
/// </summary>
public class OllamaResponse : ExternalLlmResponseBase
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public ExternalLlmMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("context")]
    public int[]? Context { get; set; }

    [JsonPropertyName("total_duration")]
    public long? TotalDuration { get; set; }

    [JsonPropertyName("load_duration")]
    public long? LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long? PromptEvalDuration { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }

    [JsonPropertyName("eval_duration")]
    public long? EvalDuration { get; set; }
}

// ==================== FUNCTION/TOOL CALLING MODELS ====================

/// <summary>
/// Function definition for OpenAI
/// </summary>
public class ExternalFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

/// <summary>
/// Tool definition for OpenAI tools API
/// </summary>
public class ExternalTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public ExternalFunction Function { get; set; } = new();
}

/// <summary>
/// Function call from LLM
/// </summary>
public class ExternalFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Tool call from LLM
/// </summary>
public class ExternalToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public ExternalFunctionCall Function { get; set; } = new();
}

/// <summary>
/// Response format specification
/// </summary>
public class ExternalResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // text, json_object

    [JsonPropertyName("json_schema")]
    public object? JsonSchema { get; set; }
}

// ==================== ERROR MODELS ====================

/// <summary>
/// Standard error response from LLM providers
/// </summary>
public class ExternalLlmError
{
    [JsonPropertyName("error")]
    public ExternalLlmErrorDetails Error { get; set; } = new();
}

/// <summary>
/// Error details
/// </summary>
public class ExternalLlmErrorDetails
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("param")]
    public string? Param { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

// ==================== STREAMING MODELS ====================

/// <summary>
/// Server-sent event for streaming responses
/// </summary>
public class ExternalLlmStreamEvent
{
    public string Event { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int? Retry { get; set; }
}

/// <summary>
/// Streaming chunk for all providers
/// </summary>
public class ExternalLlmStreamChunk
{
    public string ProviderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
    public ExternalLlmUsage? Usage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
