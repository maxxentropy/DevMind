namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Configuration options for LLM providers and their specific settings.
/// Supports multiple providers with provider-specific configuration sections.
/// </summary>
public class LlmProviderOptions
{
    /// <summary>
    /// The active LLM provider to use. Valid values: "openai", "anthropic", "ollama"
    /// </summary>
    public string Provider { get; set; } = "openai";

    /// <summary>
    /// Global LLM settings that apply to all providers
    /// </summary>
    public GlobalLlmSettings Global { get; set; } = new();

    /// <summary>
    /// OpenAI-specific configuration settings
    /// </summary>
    public OpenAiOptions OpenAi { get; set; } = new();

    /// <summary>
    /// Anthropic Claude-specific configuration settings
    /// </summary>
    public AnthropicOptions Anthropic { get; set; } = new();

    /// <summary>
    /// Ollama (local) specific configuration settings
    /// </summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>
    /// Azure OpenAI specific configuration settings
    /// </summary>
    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();

    /// <summary>
    /// Validates the current configuration and returns validation errors if any
    /// </summary>
    /// <returns>List of validation errors, empty if configuration is valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate provider selection
        var validProviders = new[] { "openai", "anthropic", "ollama", "azure-openai" };
        if (!validProviders.Contains(Provider.ToLowerInvariant()))
        {
            errors.Add($"Invalid provider '{Provider}'. Valid providers: {string.Join(", ", validProviders)}");
        }

        // Validate global settings
        errors.AddRange(Global.Validate());

        // Validate provider-specific settings based on selected provider
        switch (Provider.ToLowerInvariant())
        {
            case "openai":
                errors.AddRange(OpenAi.Validate());
                break;
            case "anthropic":
                errors.AddRange(Anthropic.Validate());
                break;
            case "ollama":
                errors.AddRange(Ollama.Validate());
                break;
            case "azure-openai":
                errors.AddRange(AzureOpenAi.Validate());
                break;
        }

        return errors;
    }

    /// <summary>
    /// Gets the configuration summary for logging/debugging purposes
    /// </summary>
    /// <returns>Safe configuration summary (without sensitive data)</returns>
    public string GetConfigurationSummary()
    {
        var summary = $"Provider: {Provider}, ";

        switch (Provider.ToLowerInvariant())
        {
            case "openai":
                summary += $"Model: {OpenAi.Model}, BaseUrl: {OpenAi.BaseUrl}";
                break;
            case "anthropic":
                summary += $"Model: {Anthropic.Model}, BaseUrl: {Anthropic.BaseUrl}";
                break;
            case "ollama":
                summary += $"Model: {Ollama.Model}, BaseUrl: {Ollama.BaseUrl}";
                break;
            case "azure-openai":
                summary += $"Deployment: {AzureOpenAi.DeploymentName}, Endpoint: {AzureOpenAi.Endpoint}";
                break;
        }

        summary += $", Global Timeout: {Global.DefaultTimeoutSeconds}s";
        return summary;
    }
}

/// <summary>
/// Global LLM settings that apply across all providers
/// </summary>
public class GlobalLlmSettings
{
    /// <summary>
    /// Default timeout for LLM requests in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default maximum number of retry attempts for failed requests
    /// </summary>
    public int DefaultMaxRetries { get; set; } = 3;

    /// <summary>
    /// Default delay between retry attempts in seconds
    /// </summary>
    public int DefaultRetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Whether to enable response caching to reduce API calls
    /// </summary>
    public bool EnableResponseCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of concurrent requests across all providers
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// Whether to enable detailed logging of LLM requests/responses
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable telemetry collection for performance monitoring
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Validates global settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DefaultTimeoutSeconds <= 0)
            errors.Add("DefaultTimeoutSeconds must be greater than 0");

        if (DefaultMaxRetries < 0)
            errors.Add("DefaultMaxRetries must be 0 or greater");

        if (DefaultRetryDelaySeconds < 0)
            errors.Add("DefaultRetryDelaySeconds must be 0 or greater");

        if (CacheExpirationMinutes <= 0)
            errors.Add("CacheExpirationMinutes must be greater than 0");

        if (MaxConcurrentRequests <= 0)
            errors.Add("MaxConcurrentRequests must be greater than 0");

        return errors;
    }
}

/// <summary>
/// Base class for provider-specific options with common settings
/// </summary>
public abstract class BaseLlmProviderOptions
{
    /// <summary>
    /// The model to use for this provider
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tokens in the response
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Temperature for response generation (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Top-p sampling parameter
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Request timeout in seconds (overrides global setting if set)
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Maximum retry attempts (overrides global setting if set)
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Provider-specific metadata/settings
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();

    /// <summary>
    /// Validates common provider settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    protected virtual List<string> ValidateBase()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Model))
            errors.Add("Model is required");

        if (MaxTokens <= 0)
            errors.Add("MaxTokens must be greater than 0");

        if (Temperature < 0.0 || Temperature > 2.0)
            errors.Add("Temperature must be between 0.0 and 2.0");

        if (TopP < 0.0 || TopP > 1.0)
            errors.Add("TopP must be between 0.0 and 1.0");

        if (TimeoutSeconds.HasValue && TimeoutSeconds.Value <= 0)
            errors.Add("TimeoutSeconds must be greater than 0 if specified");

        if (MaxRetries.HasValue && MaxRetries.Value < 0)
            errors.Add("MaxRetries must be 0 or greater if specified");

        return errors;
    }

    /// <summary>
    /// Abstract method for provider-specific validation
    /// </summary>
    /// <returns>List of validation errors</returns>
    public abstract List<string> Validate();
}

/// <summary>
/// Configuration for cost and usage tracking
/// </summary>
public class UsageTrackingOptions
{
    /// <summary>
    /// Whether to track token usage and costs
    /// </summary>
    public bool EnableUsageTracking { get; set; } = true;

    /// <summary>
    /// Cost per 1000 input tokens (for cost estimation)
    /// </summary>
    public decimal InputTokenCostPer1000 { get; set; } = 0.01m;

    /// <summary>
    /// Cost per 1000 output tokens (for cost estimation)
    /// </summary>
    public decimal OutputTokenCostPer1000 { get; set; } = 0.03m;

    /// <summary>
    /// Monthly budget limit in dollars (0 = no limit)
    /// </summary>
    public decimal MonthlyBudgetLimit { get; set; } = 0m;

    /// <summary>
    /// Whether to alert when approaching budget limit
    /// </summary>
    public bool EnableBudgetAlerts { get; set; } = true;

    /// <summary>
    /// Percentage of budget at which to send first warning (0.8 = 80%)
    /// </summary>
    public double BudgetWarningThreshold { get; set; } = 0.8;
}

/// <summary>
/// Configuration for fallback behavior when primary provider fails
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Whether to enable fallback to secondary providers
    /// </summary>
    public bool EnableFallback { get; set; } = false;

    /// <summary>
    /// List of fallback providers in order of preference
    /// </summary>
    public List<string> FallbackProviders { get; set; } = new();

    /// <summary>
    /// Maximum number of fallback attempts before giving up
    /// </summary>
    public int MaxFallbackAttempts { get; set; } = 2;

    /// <summary>
    /// Delay between fallback attempts in seconds
    /// </summary>
    public int FallbackDelaySeconds { get; set; } = 5;
}
