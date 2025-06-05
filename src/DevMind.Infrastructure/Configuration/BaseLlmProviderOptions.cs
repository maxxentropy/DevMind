namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Base class for provider-specific options with common settings
/// </summary>
public abstract class BaseLlmProviderOptions
{
    /// <summary>
    /// The model to use for this provider (virtual to allow override)
    /// </summary>
    public virtual string Model { get; set; } = string.Empty;

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
            errors.Add($"Model is required for {GetType().Name}");

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
