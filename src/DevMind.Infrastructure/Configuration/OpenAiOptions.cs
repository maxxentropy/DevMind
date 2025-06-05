// src/DevMind.Infrastructure/Configuration/OpenAiOptions.cs

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Configuration options specific to OpenAI API integration.
/// Handles OpenAI-specific settings, models, and API requirements.
/// </summary>
public class OpenAiOptions : BaseLlmProviderOptions
{
    /// <summary>
    /// OpenAI API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for OpenAI API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Organization ID for OpenAI API (optional)
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Project ID for OpenAI API (optional, for project-based billing)
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// OpenAI model to use. Defaults to GPT-4
    /// </summary>
    public override string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Frequency penalty for reducing repetition (-2.0 to 2.0)
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Presence penalty for encouraging new topics (-2.0 to 2.0)
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;

    /// <summary>
    /// Custom stop sequences for OpenAI responses
    /// </summary>
    public List<string> StopSequences { get; set; } = new();

    /// <summary>
    /// Whether to stream responses (for real-time applications)
    /// </summary>
    public bool EnableStreaming { get; set; } = false;

    /// <summary>
    /// Number of chat completion choices to generate
    /// </summary>
    public int NumberOfChoices { get; set; } = 1;

    /// <summary>
    /// Logit bias for token probability adjustment
    /// </summary>
    public Dictionary<string, double> LogitBias { get; set; } = new();

    /// <summary>
    /// Whether to return logprobs in the response
    /// </summary>
    public bool IncludeLogprobs { get; set; } = false;

    /// <summary>
    /// Number of most likely tokens to return (when IncludeLogprobs is true)
    /// </summary>
    public int TopLogprobs { get; set; } = 0;

    /// <summary>
    /// User identifier for abuse detection and monitoring
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// OpenAI-specific safety settings
    /// </summary>
    public OpenAiSafetySettings Safety { get; set; } = new();

    /// <summary>
    /// OpenAI-specific cost tracking settings
    /// </summary>
    public OpenAiCostSettings Cost { get; set; } = new();

    /// <summary>
    /// Function calling configuration for tool use
    /// </summary>
    public OpenAiFunctionSettings Functions { get; set; } = new();

    /// <summary>
    /// Available OpenAI models with their specifications
    /// </summary>
    public static readonly Dictionary<string, OpenAiModelInfo> AvailableModels = new()
    {
        ["gpt-4"] = new()
        {
            Name = "GPT-4",
            MaxTokens = 8192,
            ContextWindow = 8192,
            InputCostPer1000Tokens = 0.03m,
            OutputCostPer1000Tokens = 0.06m,
            Capabilities = new[] { "reasoning", "coding", "analysis", "creative-writing", "function-calling" },
            IsLatest = false,
            SupportsVision = false,
            SupportsFunction = true
        },
        ["gpt-4-mini"] = new()
        {
            Name = "GPT-4 Mini",
            MaxTokens = 2048,
            ContextWindow = 128000,
            InputCostPer1000Tokens = 0.01m,
            OutputCostPer1000Tokens = 0.03m,
            Capabilities = new[] { "reasoning", "coding", "analysis", "creative-writing", "function-calling" },
            IsLatest = true,
            SupportsVision = true,
            SupportsFunction = true
        },
        ["gpt-4-turbo"] = new()
        {
            Name = "GPT-4 Turbo",
            MaxTokens = 4096,
            ContextWindow = 128000,
            InputCostPer1000Tokens = 0.01m,
            OutputCostPer1000Tokens = 0.03m,
            Capabilities = new[] { "reasoning", "coding", "analysis", "creative-writing", "function-calling", "vision" },
            IsLatest = true,
            SupportsVision = true,
            SupportsFunction = true
        },
        ["gpt-4-turbo-preview"] = new()
        {
            Name = "GPT-4 Turbo Preview",
            MaxTokens = 4096,
            ContextWindow = 128000,
            InputCostPer1000Tokens = 0.01m,
            OutputCostPer1000Tokens = 0.03m,
            Capabilities = new[] { "reasoning", "coding", "analysis", "creative-writing", "function-calling" },
            IsLatest = false,
            SupportsVision = false,
            SupportsFunction = true
        },
        ["gpt-3.5-turbo"] = new()
        {
            Name = "GPT-3.5 Turbo",
            MaxTokens = 4096,
            ContextWindow = 16385,
            InputCostPer1000Tokens = 0.0015m,
            OutputCostPer1000Tokens = 0.002m,
            Capabilities = new[] { "fast-responses", "coding", "analysis", "function-calling" },
            IsLatest = false,
            SupportsVision = false,
            SupportsFunction = true
        },
        ["gpt-3.5-turbo-16k"] = new()
        {
            Name = "GPT-3.5 Turbo 16K",
            MaxTokens = 16384,
            ContextWindow = 16385,
            InputCostPer1000Tokens = 0.003m,
            OutputCostPer1000Tokens = 0.004m,
            Capabilities = new[] { "fast-responses", "coding", "analysis", "function-calling" },
            IsLatest = false,
            SupportsVision = false,
            SupportsFunction = true
        }
    };

    /// <summary>
    /// Gets the model information for the currently configured model
    /// </summary>
    public OpenAiModelInfo? GetCurrentModelInfo()
    {
        return AvailableModels.TryGetValue(Model, out var info) ? info : null;
    }

    /// <summary>
    /// Gets the recommended model for a specific task type
    /// </summary>
    /// <param name="taskType">The type of task (reasoning, coding, fast, etc.)</param>
    /// <returns>Recommended model name or null if no specific recommendation</returns>
    public static string? GetRecommendedModelForTask(string taskType)
    {
        return taskType.ToLowerInvariant() switch
        {
            "reasoning" or "complex-analysis" => "gpt-4-turbo",
            "coding" or "development" => "gpt-4-turbo",
            "analysis" or "general" => "gpt-4-turbo",
            "fast" or "simple" or "quick" => "gpt-3.5-turbo",
            "vision" or "image-analysis" => "gpt-4-turbo",
            "function-calling" or "tools" => "gpt-4-turbo",
            "cost-effective" => "gpt-3.5-turbo",
            _ => "gpt-4-turbo" // Default to GPT-4 Turbo
        };
    }

    /// <summary>
    /// Validates OpenAI-specific configuration settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public override List<string> Validate()
    {
        var errors = ValidateBase();

        // Validate API key
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            errors.Add("OpenAI ApiKey is required");
        }
        else if (!ApiKey.StartsWith("sk-"))
        {
            errors.Add("OpenAI ApiKey should start with 'sk-'");
        }

        // Validate base URL
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            errors.Add("BaseUrl is required");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("BaseUrl must be a valid HTTP/HTTPS URL");
        }

        // Validate model
        if (!string.IsNullOrWhiteSpace(Model) && !AvailableModels.ContainsKey(Model))
        {
            var availableModels = string.Join(", ", AvailableModels.Keys);
            errors.Add($"Invalid OpenAI model '{Model}'. Available models: {availableModels}");
        }

        // Validate penalties
        if (FrequencyPenalty < -2.0 || FrequencyPenalty > 2.0)
        {
            errors.Add("FrequencyPenalty must be between -2.0 and 2.0");
        }

        if (PresencePenalty < -2.0 || PresencePenalty > 2.0)
        {
            errors.Add("PresencePenalty must be between -2.0 and 2.0");
        }

        // Validate number of choices
        if (NumberOfChoices < 1 || NumberOfChoices > 10)
        {
            errors.Add("NumberOfChoices must be between 1 and 10");
        }

        // Validate top logprobs
        if (IncludeLogprobs && (TopLogprobs < 0 || TopLogprobs > 20))
        {
            errors.Add("TopLogprobs must be between 0 and 20 when IncludeLogprobs is enabled");
        }

        // Validate MaxTokens against model limits
        var modelInfo = GetCurrentModelInfo();
        if (modelInfo != null && MaxTokens > modelInfo.MaxTokens)
        {
            errors.Add($"MaxTokens ({MaxTokens}) exceeds model limit ({modelInfo.MaxTokens}) for {Model}");
        }

        // Validate logit bias values
        foreach (var bias in LogitBias)
        {
            if (bias.Value < -100 || bias.Value > 100)
            {
                errors.Add($"LogitBias value for token '{bias.Key}' must be between -100 and 100");
            }
        }

        // Validate safety settings
        errors.AddRange(Safety.Validate());

        // Validate cost settings
        errors.AddRange(Cost.Validate());

        // Validate function settings
        errors.AddRange(Functions.Validate());

        return errors;
    }

    /// <summary>
    /// Gets a safe configuration summary for logging (excludes API key)
    /// </summary>
    /// <returns>Configuration summary without sensitive data</returns>
    public string GetSafeConfigurationSummary()
    {
        var modelInfo = GetCurrentModelInfo();
        var hasApiKey = !string.IsNullOrWhiteSpace(ApiKey);

        return $"Model: {Model} ({modelInfo?.Name ?? "Unknown"}), " +
               $"BaseUrl: {BaseUrl}, " +
               $"MaxTokens: {MaxTokens}, " +
               $"Temperature: {Temperature}, " +
               $"FrequencyPenalty: {FrequencyPenalty}, " +
               $"PresencePenalty: {PresencePenalty}, " +
               $"ApiKey: {(hasApiKey ? "***configured***" : "NOT SET")}, " +
               $"Streaming: {EnableStreaming}, " +
               $"Organization: {OrganizationId ?? "none"}";
    }

    /// <summary>
    /// Creates a copy of the options with API key redacted for logging
    /// </summary>
    /// <returns>Copy of options with sensitive data removed</returns>
    public OpenAiOptions CreateRedactedCopy()
    {
        var copy = new OpenAiOptions
        {
            Model = Model,
            BaseUrl = BaseUrl,
            OrganizationId = OrganizationId,
            ProjectId = ProjectId,
            MaxTokens = MaxTokens,
            Temperature = Temperature,
            TopP = TopP,
            FrequencyPenalty = FrequencyPenalty,
            PresencePenalty = PresencePenalty,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetries = MaxRetries,
            Enabled = Enabled,
            StopSequences = new List<string>(StopSequences),
            EnableStreaming = EnableStreaming,
            NumberOfChoices = NumberOfChoices,
            LogitBias = new Dictionary<string, double>(LogitBias),
            IncludeLogprobs = IncludeLogprobs,
            TopLogprobs = TopLogprobs,
            UserId = UserId,
            ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? string.Empty : "***REDACTED***",
            Safety = Safety,
            Cost = Cost,
            Functions = Functions,
            AdditionalSettings = new Dictionary<string, object>(AdditionalSettings)
        };
        return copy;
    }

    /// <summary>
    /// Checks if the current model supports a specific capability
    /// </summary>
    /// <param name="capability">The capability to check for</param>
    /// <returns>True if the model supports the capability</returns>
    public bool SupportsCapability(string capability)
    {
        var modelInfo = GetCurrentModelInfo();
        return modelInfo?.Capabilities?.Contains(capability.ToLowerInvariant()) ?? false;
    }
}

/// <summary>
/// Information about a specific OpenAI model
/// </summary>
public class OpenAiModelInfo
{
    public string Name { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public decimal InputCostPer1000Tokens { get; set; }
    public decimal OutputCostPer1000Tokens { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public bool IsLatest { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsFunction { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleasedDate { get; set; }
}

/// <summary>
/// OpenAI-specific safety and content filtering settings
/// </summary>
public class OpenAiSafetySettings
{
    /// <summary>
    /// Whether to enable OpenAI's moderation endpoint
    /// </summary>
    public bool EnableModeration { get; set; } = true;

    /// <summary>
    /// Whether to automatically filter unsafe content
    /// </summary>
    public bool AutoFilterUnsafeContent { get; set; } = true;

    /// <summary>
    /// Custom content policies to enforce
    /// </summary>
    public List<string> CustomContentPolicies { get; set; } = new();

    /// <summary>
    /// Whether to log safety violations for monitoring
    /// </summary>
    public bool LogSafetyViolations { get; set; } = true;

    /// <summary>
    /// Maximum allowed toxicity score (0.0 to 1.0)
    /// </summary>
    public double MaxToxicityScore { get; set; } = 0.7;

    /// <summary>
    /// Validates safety settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxToxicityScore < 0.0 || MaxToxicityScore > 1.0)
        {
            errors.Add("MaxToxicityScore must be between 0.0 and 1.0");
        }

        return errors;
    }
}

/// <summary>
/// OpenAI-specific cost tracking and management settings
/// </summary>
public class OpenAiCostSettings
{
    /// <summary>
    /// Whether to track costs for OpenAI API calls
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;

    /// <summary>
    /// Daily spending limit in USD (0 = no limit)
    /// </summary>
    public decimal DailySpendingLimit { get; set; } = 0m;

    /// <summary>
    /// Monthly spending limit in USD (0 = no limit)
    /// </summary>
    public decimal MonthlySpendingLimit { get; set; } = 100m;

    /// <summary>
    /// Whether to auto-optimize model selection based on cost
    /// </summary>
    public bool AutoOptimizeModelSelection { get; set; } = false;

    /// <summary>
    /// Preferred model for cost optimization (when auto-optimize is enabled)
    /// </summary>
    public string CostOptimizedModel { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Whether to enable batch processing for cost savings
    /// </summary>
    public bool EnableBatchProcessing { get; set; } = false;

    /// <summary>
    /// Minimum batch size for batch processing
    /// </summary>
    public int MinBatchSize { get; set; } = 10;

    /// <summary>
    /// Whether to alert when approaching spending limits
    /// </summary>
    public bool EnableSpendingAlerts { get; set; } = true;

    /// <summary>
    /// Percentage of limit at which to send spending alerts (0.8 = 80%)
    /// </summary>
    public double SpendingAlertThreshold { get; set; } = 0.8;

    /// <summary>
    /// Validates cost settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DailySpendingLimit < 0)
            errors.Add("DailySpendingLimit must be 0 or greater");

        if (MonthlySpendingLimit < 0)
            errors.Add("MonthlySpendingLimit must be 0 or greater");

        if (SpendingAlertThreshold < 0 || SpendingAlertThreshold > 1)
            errors.Add("SpendingAlertThreshold must be between 0 and 1");

        if (MinBatchSize < 1)
            errors.Add("MinBatchSize must be 1 or greater");

        if (AutoOptimizeModelSelection &&
            !string.IsNullOrWhiteSpace(CostOptimizedModel) &&
            !OpenAiOptions.AvailableModels.ContainsKey(CostOptimizedModel))
        {
            errors.Add($"CostOptimizedModel '{CostOptimizedModel}' is not a valid OpenAI model");
        }

        return errors;
    }
}

/// <summary>
/// OpenAI function calling configuration
/// </summary>
public class OpenAiFunctionSettings
{
    /// <summary>
    /// Whether to enable function calling
    /// </summary>
    public bool EnableFunctionCalling { get; set; } = true;

    /// <summary>
    /// Maximum number of function calls per conversation
    /// </summary>
    public int MaxFunctionCalls { get; set; } = 10;

    /// <summary>
    /// Whether to automatically call functions or require explicit approval
    /// </summary>
    public bool AutoCallFunctions { get; set; } = true;

    /// <summary>
    /// Timeout for function execution in seconds
    /// </summary>
    public int FunctionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to include function results in conversation context
    /// </summary>
    public bool IncludeFunctionResults { get; set; } = true;

    /// <summary>
    /// Custom function call policies
    /// </summary>
    public List<string> AllowedFunctions { get; set; } = new();

    /// <summary>
    /// Functions that are explicitly forbidden
    /// </summary>
    public List<string> ForbiddenFunctions { get; set; } = new();

    /// <summary>
    /// Validates function settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxFunctionCalls < 0)
            errors.Add("MaxFunctionCalls must be 0 or greater");

        if (FunctionTimeoutSeconds <= 0)
            errors.Add("FunctionTimeoutSeconds must be greater than 0");

        return errors;
    }
}
