using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Comprehensive CLI configuration validator
/// </summary>
public class ComprehensiveCliValidator : ICliConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComprehensiveCliValidator> _logger;

    public ComprehensiveCliValidator(IConfiguration configuration, ILogger<ComprehensiveCliValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<string>> ValidateAllAsync()
    {
        var allErrors = new List<string>();

        allErrors.AddRange(await ValidateLlmConfigurationAsync());
        allErrors.AddRange(await ValidateMcpConfigurationAsync());
        allErrors.AddRange(await ValidateInfrastructureConfigurationAsync());

        return allErrors;
    }

    public async Task<List<string>> ValidateLlmConfigurationAsync()
    {
        await Task.CompletedTask; // Async for consistency
        var errors = new List<string>();

        try
        {
            var llmSection = _configuration.GetSection("Llm");
            if (!llmSection.Exists())
            {
                errors.Add("LLM configuration section is missing");
                return errors;
            }

            var provider = llmSection["Provider"];
            if (string.IsNullOrWhiteSpace(provider))
            {
                errors.Add("LLM provider is not specified");
            }
            else
            {
                errors.AddRange(ValidateProviderConfiguration(provider, llmSection));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating LLM configuration");
            errors.Add($"Error validating LLM configuration: {ex.Message}");
        }

        return errors;
    }

    public async Task<List<string>> ValidateMcpConfigurationAsync()
    {
        await Task.CompletedTask; // Async for consistency
        var errors = new List<string>();

        try
        {
            var mcpSection = _configuration.GetSection("McpClient");
            if (!mcpSection.Exists())
            {
                errors.Add("MCP client configuration section is missing");
                return errors;
            }

            var baseUrl = mcpSection["BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                errors.Add("MCP client BaseUrl is not configured");
            }
            else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                errors.Add($"MCP client BaseUrl '{baseUrl}' is not a valid URL");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MCP configuration");
            errors.Add($"Error validating MCP configuration: {ex.Message}");
        }

        return errors;
    }

    public async Task<List<string>> ValidateInfrastructureConfigurationAsync()
    {
        await Task.CompletedTask; // Async for consistency
        var errors = new List<string>();

        try
        {
            // Validate logging configuration
            var loggingSection = _configuration.GetSection("Logging");
            if (!loggingSection.Exists())
            {
                errors.Add("Logging configuration section is missing");
            }

            // Validate any other infrastructure settings
            // Add more validation as needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating infrastructure configuration");
            errors.Add($"Error validating infrastructure configuration: {ex.Message}");
        }

        return errors;
    }

    private List<string> ValidateProviderConfiguration(string provider, IConfigurationSection llmSection)
    {
        var errors = new List<string>();

        switch (provider.ToLowerInvariant())
        {
            case "openai":
                errors.AddRange(ValidateOpenAiConfiguration(llmSection.GetSection("OpenAi")));
                break;
            case "anthropic":
                errors.AddRange(ValidateAnthropicConfiguration(llmSection.GetSection("Anthropic")));
                break;
            case "ollama":
                errors.AddRange(ValidateOllamaConfiguration(llmSection.GetSection("Ollama")));
                break;
            case "azure-openai":
                errors.AddRange(ValidateAzureOpenAiConfiguration(llmSection.GetSection("AzureOpenAi")));
                break;
            default:
                errors.Add($"Unknown LLM provider: {provider}");
                break;
        }

        return errors;
    }

    private List<string> ValidateOpenAiConfiguration(IConfigurationSection section)
    {
        var errors = new List<string>();

        if (!section.Exists())
        {
            errors.Add("OpenAI configuration section is missing");
            return errors;
        }

        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add("OpenAI API key is not configured");
        }
        else if (!apiKey.StartsWith("sk-"))
        {
            errors.Add("OpenAI API key format appears invalid (should start with 'sk-')");
        }

        return errors;
    }

    private List<string> ValidateAnthropicConfiguration(IConfigurationSection section)
    {
        var errors = new List<string>();

        if (!section.Exists())
        {
            errors.Add("Anthropic configuration section is missing");
            return errors;
        }

        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add("Anthropic API key is not configured");
        }
        else if (!apiKey.StartsWith("sk-ant-"))
        {
            errors.Add("Anthropic API key format appears invalid (should start with 'sk-ant-')");
        }

        return errors;
    }

    private List<string> ValidateOllamaConfiguration(IConfigurationSection section)
    {
        var errors = new List<string>();

        if (!section.Exists())
        {
            errors.Add("Ollama configuration section is missing");
            return errors;
        }

        var baseUrl = section["BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            errors.Add("Ollama BaseUrl is not configured");
        }
        else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            errors.Add($"Ollama BaseUrl '{baseUrl}' is not a valid URL");
        }

        return errors;
    }

    private List<string> ValidateAzureOpenAiConfiguration(IConfigurationSection section)
    {
        var errors = new List<string>();

        if (!section.Exists())
        {
            errors.Add("Azure OpenAI configuration section is missing");
            return errors;
        }

        var endpoint = section["Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            errors.Add("Azure OpenAI endpoint is not configured");
        }
        else if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            errors.Add($"Azure OpenAI endpoint '{endpoint}' is not a valid URL");
        }

        return errors;
    }
}
