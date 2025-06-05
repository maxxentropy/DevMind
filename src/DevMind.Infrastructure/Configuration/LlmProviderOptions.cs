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
