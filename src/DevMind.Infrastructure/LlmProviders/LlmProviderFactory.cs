using DevMind.Core.Application.Interfaces;
using DevMind.Infrastructure.Configuration;
using DevMind.Infrastructure.LlmProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DevMind.Infrastructure.Configuration.LlmServiceExtensions;

/// <summary>
/// Factory implementation for creating LLM provider instances
/// </summary>
public class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LlmProviderFactory> _logger;
    private readonly IOptions<LlmProviderOptions> _options;

    public LlmProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<LlmProviderFactory> logger,
        IOptions<LlmProviderOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    public ILlmService CreateProvider(string providerName)
    {
        try
        {
            ILlmService? provider = providerName.ToLowerInvariant() switch
            {
                "openai" => _serviceProvider.GetRequiredService<OpenAiService>(),
                "anthropic" => _serviceProvider.GetRequiredService<AnthropicService>(),
                "ollama" => _serviceProvider.GetRequiredService<OllamaService>(),
                //"azure-openai" => _serviceProvider.GetRequiredService<AzureOpenAiService>(),
                _ => throw new ArgumentException($"Unknown LLM provider: {providerName}")
            };

            _logger.LogDebug("Created LLM provider: {ProviderName}", providerName);
            return provider!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create LLM provider: {ProviderName}", providerName);
            throw;
        }
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return new[] { "openai", "anthropic", "ollama", "azure-openai" };
    }

    public bool IsProviderAvailable(string providerName)
    {
        try
        {
            CreateProvider(providerName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
