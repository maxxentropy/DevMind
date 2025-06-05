// src/DevMind.Infrastructure/Configuration/LlmConfigurationValidator.cs

using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Validates LLM configuration on application startup and provides detailed feedback
/// </summary>
public class LlmConfigurationValidator : IHostedService
{
    #region Private Fields

    private readonly ILogger<LlmConfigurationValidator> _logger;
    private readonly IOptions<LlmProviderOptions> _llmOptions;
    private readonly IOptions<OpenAiOptions> _openAiOptions;
    private readonly IOptions<AnthropicOptions> _anthropicOptions;
    private readonly IOptions<OllamaOptions> _ollamaOptions;
    private readonly IOptions<AzureOpenAiOptions> _azureOpenAiOptions;

    #endregion

    #region Constructor

    public LlmConfigurationValidator(
        ILogger<LlmConfigurationValidator> logger,
        IOptions<LlmProviderOptions> llmOptions,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<AnthropicOptions> anthropicOptions,
        IOptions<OllamaOptions> ollamaOptions,
        IOptions<AzureOpenAiOptions> azureOpenAiOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmOptions = llmOptions ?? throw new ArgumentNullException(nameof(llmOptions));
        _openAiOptions = openAiOptions ?? throw new ArgumentNullException(nameof(openAiOptions));
        _anthropicOptions = anthropicOptions ?? throw new ArgumentNullException(nameof(anthropicOptions));
        _ollamaOptions = ollamaOptions ?? throw new ArgumentNullException(nameof(ollamaOptions));
        _azureOpenAiOptions = azureOpenAiOptions ?? throw new ArgumentNullException(nameof(azureOpenAiOptions));
    }

    #endregion

    #region Public Methods

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating LLM configuration...");

        var allErrors = new List<string>();

        // Validate main LLM configuration
        var mainErrors = ValidateMainConfiguration();
        allErrors.AddRange(mainErrors);

        // Validate provider-specific configurations
        var providerErrors = ValidateProviderConfigurations();
        allErrors.AddRange(providerErrors);

        // Validate connectivity requirements
        var connectivityErrors = ValidateConnectivityRequirements();
        allErrors.AddRange(connectivityErrors);

        if (allErrors.Any())
        {
            var errorMessage = $"LLM Configuration validation failed:\n{string.Join("\n", allErrors.Select((error, index) => $"{index + 1}. {error}"))}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("LLM configuration validation completed successfully");
        LogConfigurationSummary();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private List<string> ValidateMainConfiguration()
    {
        var errors = new List<string>();

        try
        {
            var options = _llmOptions.Value;
            errors.AddRange(options.Validate());
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to load main LLM configuration: {ex.Message}");
        }

        return errors;
    }

    private List<string> ValidateProviderConfigurations()
    {
        var errors = new List<string>();
        var selectedProvider = _llmOptions.Value.Provider.ToLowerInvariant();

        // Validate the selected provider's configuration
        switch (selectedProvider)
        {
            case "openai":
                errors.AddRange(ValidateOpenAiConfiguration());
                break;
            case "anthropic":
                errors.AddRange(ValidateAnthropicConfiguration());
                break;
            case "ollama":
                errors.AddRange(ValidateOllamaConfiguration());
                break;
            case "azure-openai":
                errors.AddRange(ValidateAzureOpenAiConfiguration());
                break;
            default:
                errors.Add($"Unknown provider selected: {selectedProvider}");
                break;
        }

        return errors;
    }

    private List<string> ValidateOpenAiConfiguration()
    {
        try
        {
            var options = _openAiOptions.Value;
            return options.Validate();
        }
        catch (Exception ex)
        {
            return new List<string> { $"Failed to load OpenAI configuration: {ex.Message}" };
        }
    }

    private List<string> ValidateAnthropicConfiguration()
    {
        try
        {
            var options = _anthropicOptions.Value;
            return options.Validate();
        }
        catch (Exception ex)
        {
            return new List<string> { $"Failed to load Anthropic configuration: {ex.Message}" };
        }
    }

    private List<string> ValidateOllamaConfiguration()
    {
        try
        {
            var options = _ollamaOptions.Value;
            return options.Validate();
        }
        catch (Exception ex)
        {
            return new List<string> { $"Failed to load Ollama configuration: {ex.Message}" };
        }
    }

    private List<string> ValidateAzureOpenAiConfiguration()
    {
        try
        {
            var options = _azureOpenAiOptions.Value;
            return options.Validate();
        }
        catch (Exception ex)
        {
            return new List<string> { $"Failed to load Azure OpenAI configuration: {ex.Message}" };
        }
    }

    private List<string> ValidateConnectivityRequirements()
    {
        var errors = new List<string>();
        var selectedProvider = _llmOptions.Value.Provider.ToLowerInvariant();

        // Validate that required network access is available
        switch (selectedProvider)
        {
            case "openai":
                if (!IsValidUri(_openAiOptions.Value.BaseUrl))
                {
                    errors.Add($"OpenAI BaseUrl is not a valid URI: {_openAiOptions.Value.BaseUrl}");
                }
                break;
            case "anthropic":
                if (!IsValidUri(_anthropicOptions.Value.BaseUrl))
                {
                    errors.Add($"Anthropic BaseUrl is not a valid URI: {_anthropicOptions.Value.BaseUrl}");
                }
                break;
            case "ollama":
                if (!IsValidUri(_ollamaOptions.Value.BaseUrl))
                {
                    errors.Add($"Ollama BaseUrl is not a valid URI: {_ollamaOptions.Value.BaseUrl}");
                }
                break;
            case "azure-openai":
                if (!IsValidUri(_azureOpenAiOptions.Value.Endpoint))
                {
                    errors.Add($"Azure OpenAI Endpoint is not a valid URI: {_azureOpenAiOptions.Value.Endpoint}");
                }
                break;
        }

        return errors;
    }

    private static bool IsValidUri(string uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private void LogConfigurationSummary()
    {
        var summary = _llmOptions.Value.GetConfigurationSummary();
        _logger.LogInformation("LLM Configuration Summary: {Summary}", summary);

        // Log provider-specific details
        var selectedProvider = _llmOptions.Value.Provider.ToLowerInvariant();
        switch (selectedProvider)
        {
            case "openai":
                _logger.LogDebug("OpenAI Configuration: {Configuration}", _openAiOptions.Value.GetSafeConfigurationSummary());
                break;
            case "anthropic":
                _logger.LogDebug("Anthropic Configuration: {Configuration}", _anthropicOptions.Value.GetSafeConfigurationSummary());
                break;
            case "ollama":
                _logger.LogDebug("Ollama Configuration: {Configuration}", _ollamaOptions.Value.GetSafeConfigurationSummary());
                break;
            case "azure-openai":
                _logger.LogDebug("Azure OpenAI Configuration: {Configuration}", _azureOpenAiOptions.Value.GetSafeConfigurationSummary());
                break;
        }
    }

    #endregion
}
