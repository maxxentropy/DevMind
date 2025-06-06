// src/DevMind.CLI/Commands/ConfigCommand.cs

using DevMind.CLI.Interfaces;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for displaying and validating configuration settings
/// </summary>
public class ConfigCommand
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<LlmProviderOptions> _llmOptions;
    private readonly IConsoleService _console;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(
        IConfiguration configuration,
        IOptions<LlmProviderOptions> llmOptions,
        IConsoleService console,
        ILogger<ConfigCommand> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _llmOptions = llmOptions ?? throw new ArgumentNullException(nameof(llmOptions));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await ShowHelpAsync();
            return 0;
        }

        return args[0].ToLowerInvariant() switch
        {
            "validate" => await ValidateConfigurationAsync(),
            "show" => await ShowConfigurationAsync(args.Length > 1 ? args[1] : null),
            "providers" => await ShowProvidersAsync(),
            _ => await ShowHelpAsync(),
        };
    }

    private async Task<int> ValidateConfigurationAsync()
    {
        await _console.WriteBannerAsync("Configuration Validation");
        var hasErrors = false;

        try
        {
            await _console.WriteAsync("Validating LLM Provider Configuration... ");
            var llmOptions = _llmOptions.Value;
            var errors = llmOptions.Validate();

            if (errors.Any())
            {
                await _console.WriteLineAsync("❌ INVALID", ConsoleColor.Red);
                foreach (var error in errors)
                {
                    await _console.WriteErrorAsync($"  - {error}");
                }
                hasErrors = true;
            }
            else
            {
                await _console.WriteLineAsync("✅ VALID", ConsoleColor.Green);
            }
        }
        catch (Exception ex)
        {
            await _console.WriteLineAsync("❌ ERROR", ConsoleColor.Red);
            await _console.WriteErrorAsync($"  - {ex.Message}");
            hasErrors = true;
        }

        await _console.WriteLineAsync();
        if (hasErrors)
        {
            await _console.WriteErrorAsync("Configuration validation failed. Please fix the errors in your appsettings.json or environment variables.");
            return 1;
        }

        await _console.WriteSuccessAsync("All configurations are valid!");
        return 0;
    }

    private async Task<int> ShowConfigurationAsync(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            await _console.WriteBannerAsync("All Configurations");
            await ShowLlmConfigurationAsync();
            await ShowMcpConfigurationAsync();
            return 0;
        }

        var configSection = _configuration.GetSection(section);
        if (!configSection.Exists())
        {
            await _console.WriteErrorAsync($"Configuration section '{section}' not found.");
            return 1;
        }

        await _console.WriteBannerAsync($"Configuration Section: {section}");
        foreach (var child in configSection.GetChildren())
        {
            await PrintConfigurationValueAsync(child, "");
        }
        return 0;
    }

    private async Task PrintConfigurationValueAsync(IConfigurationSection section, string indent)
    {
        var keyColor = ConsoleColor.Cyan;
        var valueColor = ConsoleColor.White;

        if (section.GetChildren().Any())
        {
            await _console.WriteLineAsync($"{indent}{section.Key}:", keyColor);
            foreach (var child in section.GetChildren())
            {
                await PrintConfigurationValueAsync(child, indent + "  ");
            }
        }
        else
        {
            var value = section.Value;
            if (IsSensitiveKey(section.Key))
            {
                value = string.IsNullOrWhiteSpace(value) ? "NOT SET" : "***REDACTED***";
                valueColor = ConsoleColor.DarkYellow;
            }
            await _console.WriteAsync($"{indent}{section.Key}: ", keyColor);
            await _console.WriteLineAsync(value ?? "null", valueColor);
        }
    }

    private bool IsSensitiveKey(string key) =>
        new[] { "apikey", "secret", "password", "token" }.Any(s => key.ToLowerInvariant().Contains(s));

    private async Task ShowLlmConfigurationAsync()
    {
        await _console.WriteBannerAsync("LLM Provider Configuration");
        var llmOptions = _llmOptions.Value;
        await _console.WriteKeyValueAsync("Active Provider", llmOptions.Provider, 20, ConsoleColor.Yellow);
        await _console.WriteKeyValueAsync("Config Summary", llmOptions.GetConfigurationSummary(), 20, ConsoleColor.Yellow);
    }

    private async Task ShowMcpConfigurationAsync()
    {
        await _console.WriteBannerAsync("MCP Client Configuration");
        var mcpSection = _configuration.GetSection("McpClient");
        if (mcpSection.Exists())
        {
            await _console.WriteKeyValueAsync("Base URL", mcpSection["BaseUrl"], 20);
            await _console.WriteKeyValueAsync("Timeout", $"{mcpSection["TimeoutSeconds"]}s", 20);
        }
    }

    private async Task<int> ShowProvidersAsync()
    {
        await _console.WriteBannerAsync("Available LLM Providers");
        var providers = new[] {
            ("openai", "Supports GPT-4, GPT-3.5 models. Good for general purpose tasks and reasoning."),
            ("anthropic", "Supports Claude models. Strong in analysis and coding."),
            ("ollama", "Supports local models like Llama, CodeLlama. Best for privacy and offline use."),
            ("azure-openai", "Enterprise-grade Azure-hosted OpenAI models.")
        };
        foreach (var (name, desc) in providers)
        {
            await _console.WriteAsync($"{name,-15}", ConsoleColor.Yellow);
            await _console.WriteLineAsync(desc, ConsoleColor.White);
        }
        return 0;
    }

    private async Task<int> ShowHelpAsync()
    {
        await _console.WriteBannerAsync("Config Command Help");
        await _console.WriteLineAsync("Usage: devmind config [subcommand]");
        await _console.WriteLineAsync("\nSubcommands:");
        await _console.WriteKeyValueAsync("validate", "Validate current configuration.", 12);
        await _console.WriteKeyValueAsync("show [section]", "Show all or a specific section of the configuration.", 12);
        await _console.WriteKeyValueAsync("providers", "List available LLM providers.", 12);
        return 0;
    }
}
