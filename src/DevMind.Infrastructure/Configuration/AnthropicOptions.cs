// src/DevMind.Infrastructure/Configuration/AnthropicOptions.cs

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Configuration options specific to Anthropic Claude API integration.
/// Handles Claude-specific settings, models, and API requirements.
/// </summary>
public class AnthropicOptions : BaseLlmProviderOptions
{
    /// <summary>
    /// Anthropic API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Anthropic API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>
    /// Anthropic API version header value
    /// </summary>
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>
    /// Claude model to use. Defaults to Claude 3 Sonnet
    /// </summary>
    public override string Model { get; set; } = "claude-3-sonnet-20240229";

    /// <summary>
    /// Top-k sampling parameter for Claude (affects randomness)
    /// </summary>
    public int TopK { get; set; } = 40;

    /// <summary>
    /// Custom stop sequences for Claude responses
    /// </summary>
    public List<string> StopSequences { get; set; } = new();

    /// <summary>
    /// Whether to use the beta features of Claude API
    /// </summary>
    public bool UseBetaFeatures { get; set; } = false;

    /// <summary>
    /// Maximum number of tokens in the system prompt
    /// </summary>
    public int MaxSystemPromptTokens { get; set; } = 1000;

    /// <summary>
    /// Claude-specific safety settings
    /// </summary>
    public ClaudeSafetySettings Safety { get; set; } = new();

    /// <summary>
    /// Claude-specific cost tracking settings
    /// </summary>
    public ClaudeCostSettings Cost { get; set; } = new();

    /// <summary>
    /// Available Claude models with their specifications
    /// </summary>
    public static readonly Dictionary<string, ClaudeModelInfo> AvailableModels = new()
    {
        ["claude-3-opus-20240229"] = new()
        {
            Name = "Claude 3 Opus",
            MaxTokens = 200000,
            ContextWindow = 200000,
            InputCostPer1000Tokens = 0.015m,
            OutputCostPer1000Tokens = 0.075m,
            Capabilities = new[] { "reasoning", "coding", "analysis", "creative-writing" },
            IsLatest = false
        },
        ["claude-3-sonnet-20240229"] = new()
        {
            Name = "Claude 3 Sonnet",
            MaxTokens = 200000,
            ContextWindow = 200000,
            InputCostPer1000Tokens = 0.003m,
            OutputCostPer1000Tokens = 0.015m,
            Capabilities = new[] { "reasoning", "coding", "analysis" },
            IsLatest = true
        },
        ["claude-3-haiku-20240307"] = new()
        {
            Name = "Claude 3 Haiku",
            MaxTokens = 200000,
            ContextWindow = 200000,
            InputCostPer1000Tokens = 0.00025m,
            OutputCostPer1000Tokens = 0.00125m,
            Capabilities = new[] { "fast-responses", "simple-tasks" },
            IsLatest = false
        }
    };

    /// <summary>
    /// Gets the model information for the currently configured model
    /// </summary>
    public ClaudeModelInfo? GetCurrentModelInfo()
    {
        return AvailableModels.TryGetValue(Model, out var info) ? info : null;
    }

    /// <summary>
    /// Gets the recommended model for a specific task type
    /// </summary>
    /// <param name="taskType">The type of task (reasoning, coding, analysis, etc.)</param>
    /// <returns>Recommended model name or null if no specific recommendation</returns>
    public static string? GetRecommendedModelForTask(string taskType)
    {
        return taskType.ToLowerInvariant() switch
        {
            "reasoning" or "complex-analysis" => "claude-3-opus-20240229",
            "coding" or "development" => "claude-3-sonnet-20240229",
            "analysis" or "general" => "claude-3-sonnet-20240229",
            "fast" or "simple" => "claude-3-haiku-20240307",
            _ => "claude-3-sonnet-20240229" // Default to Sonnet
        };
    }

    /// <summary>
    /// Validates Anthropic-specific configuration settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public override List<string> Validate()
    {
        var errors = ValidateBase();

        // Validate API key
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            errors.Add("Anthropic ApiKey is required");
        }
        else if (!ApiKey.StartsWith("sk-ant-"))
        {
            errors.Add("Anthropic ApiKey should start with 'sk-ant-'");
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

        // Validate API version
        if (string.IsNullOrWhiteSpace(AnthropicVersion))
        {
            errors.Add("AnthropicVersion is required");
        }

        // Validate model
        if (!string.IsNullOrWhiteSpace(Model) && !AvailableModels.ContainsKey(Model))
        {
            var availableModels = string.Join(", ", AvailableModels.Keys);
            errors.Add($"Invalid Claude model '{Model}'. Available models: {availableModels}");
        }

        // Validate TopK
        if (TopK <= 0 || TopK > 100)
        {
            errors.Add("TopK must be between 1 and 100");
        }

        // Validate MaxTokens against model limits
        var modelInfo = GetCurrentModelInfo();
        if (modelInfo != null && MaxTokens > modelInfo.MaxTokens)
        {
            errors.Add($"MaxTokens ({MaxTokens}) exceeds model limit ({modelInfo.MaxTokens}) for {Model}");
        }

        // Validate system prompt token limit
        if (MaxSystemPromptTokens <= 0 || MaxSystemPromptTokens > MaxTokens)
        {
            errors.Add("MaxSystemPromptTokens must be greater than 0 and less than MaxTokens");
        }

        // Validate safety settings
        errors.AddRange(Safety.Validate());

        // Validate cost settings
        errors.AddRange(Cost.Validate());

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
               $"Version: {AnthropicVersion}, " +
               $"MaxTokens: {MaxTokens}, " +
               $"Temperature: {Temperature}, " +
               $"TopK: {TopK}, " +
               $"ApiKey: {(hasApiKey ? "***configured***" : "NOT SET")}, " +
               $"BetaFeatures: {UseBetaFeatures}";
    }

    /// <summary>
    /// Creates a copy of the options with API key redacted for logging
    /// </summary>
    /// <returns>Copy of options with sensitive data removed</returns>
    public AnthropicOptions CreateRedactedCopy()
    {
        var copy = new AnthropicOptions
        {
            Model = Model,
            BaseUrl = BaseUrl,
            AnthropicVersion = AnthropicVersion,
            MaxTokens = MaxTokens,
            Temperature = Temperature,
            TopP = TopP,
            TopK = TopK,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetries = MaxRetries,
            Enabled = Enabled,
            StopSequences = new List<string>(StopSequences),
            UseBetaFeatures = UseBetaFeatures,
            MaxSystemPromptTokens = MaxSystemPromptTokens,
            ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? string.Empty : "***REDACTED***",
            Safety = Safety,
            Cost = Cost,
            AdditionalSettings = new Dictionary<string, object>(AdditionalSettings)
        };
        return copy;
    }
}

/// <summary>
/// Information about a specific Claude model
/// </summary>
public class ClaudeModelInfo
{
    public string Name { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public decimal InputCostPer1000Tokens { get; set; }
    public decimal OutputCostPer1000Tokens { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public bool IsLatest { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleasedDate { get; set; }
}

/// <summary>
/// Claude-specific safety and content filtering settings
/// </summary>
public class ClaudeSafetySettings
{
    /// <summary>
    /// Whether to enable Claude's built-in safety filtering
    /// </summary>
    public bool EnableSafetyFiltering { get; set; } = true;

    /// <summary>
    /// Whether to reject requests that might produce harmful content
    /// </summary>
    public bool RejectHarmfulRequests { get; set; } = true;

    /// <summary>
    /// Custom content policies to enforce
    /// </summary>
    public List<string> CustomContentPolicies { get; set; } = new();

    /// <summary>
    /// Whether to log safety violations for monitoring
    /// </summary>
    public bool LogSafetyViolations { get; set; } = true;

    /// <summary>
    /// Action to take when safety violations are detected
    /// </summary>
    public SafetyViolationAction ViolationAction { get; set; } = SafetyViolationAction.Reject;

    /// <summary>
    /// Validates safety settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Currently no specific validation rules for safety settings
        // Could add validation for custom policies format, etc.

        return errors;
    }
}

/// <summary>
/// Claude-specific cost tracking and management settings
/// </summary>
public class ClaudeCostSettings
{
    /// <summary>
    /// Whether to track costs for Claude API calls
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
    public string CostOptimizedModel { get; set; } = "claude-3-haiku-20240307";

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

        if (AutoOptimizeModelSelection &&
            !string.IsNullOrWhiteSpace(CostOptimizedModel) &&
            !AnthropicOptions.AvailableModels.ContainsKey(CostOptimizedModel))
        {
            errors.Add($"CostOptimizedModel '{CostOptimizedModel}' is not a valid Claude model");
        }

        return errors;
    }
}

/// <summary>
/// Actions to take when safety violations are detected
/// </summary>
public enum SafetyViolationAction
{
    /// <summary>
    /// Log the violation but allow the request to proceed
    /// </summary>
    LogOnly,

    /// <summary>
    /// Reject the request and return an error
    /// </summary>
    Reject,

    /// <summary>
    /// Sanitize the request and retry with modified prompt
    /// </summary>
    SanitizeAndRetry,

    /// <summary>
    /// Fall back to a different model with stricter safety controls
    /// </summary>
    FallbackToSaferModel
}
