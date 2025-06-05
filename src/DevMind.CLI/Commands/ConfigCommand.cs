using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for displaying and validating configuration settings
/// </summary>
public class ConfigCommand
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<LlmProviderOptions> _llmOptions;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(
        IConfiguration configuration,
        IOptions<LlmProviderOptions> llmOptions,
        ILogger<ConfigCommand> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _llmOptions = llmOptions ?? throw new ArgumentNullException(nameof(llmOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            await Task.CompletedTask; // Make method async for consistency

            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "validate":
                        return ValidateConfiguration();
                    case "show":
                        return ShowConfiguration(args.Length > 1 ? args[1] : null);
                    case "providers":
                        return ShowProviders();
                    default:
                        ShowHelp();
                        return 0;
                }
            }

            return ShowConfiguration();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Configuration command failed: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "Configuration command execution failed");
            return 1;
        }
    }

    private int ValidateConfiguration()
    {
        Console.WriteLine("Validating DevMind Configuration...");
        Console.WriteLine();

        var hasErrors = false;

        // Validate LLM Provider Options
        Console.Write("LLM Provider Configuration... ");
        try
        {
            var llmOptions = _llmOptions.Value;
            var errors = llmOptions.Validate();

            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ INVALID");
                Console.ResetColor();
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                hasErrors = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ VALID");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ERROR");
            Console.ResetColor();
            Console.WriteLine($"  - {ex.Message}");
            hasErrors = true;
        }

        Console.WriteLine();

        if (hasErrors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Configuration validation failed. Please fix the errors above.");
            Console.ResetColor();
            return 1;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ All configuration is valid!");
            Console.ResetColor();
            return 0;
        }
    }

    private int ShowConfiguration(string? section = null)
    {
        Console.WriteLine("DevMind Configuration");
        Console.WriteLine("====================");
        Console.WriteLine();

        if (section != null)
        {
            ShowConfigurationSection(section);
        }
        else
        {
            ShowLlmConfiguration();
            Console.WriteLine();
            ShowMcpConfiguration();
            Console.WriteLine();
            ShowAgentConfiguration();
        }

        return 0;
    }

    private void ShowConfigurationSection(string section)
    {
        var config = _configuration.GetSection(section);
        if (!config.Exists())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Configuration section '{section}' not found.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"{section} Configuration:");
        Console.WriteLine(new string('-', section.Length + 15));

        foreach (var child in config.GetChildren())
        {
            PrintConfigurationValue(child, "");
        }
    }

    private void PrintConfigurationValue(IConfigurationSection section, string indent)
    {
        if (section.GetChildren().Any())
        {
            Console.WriteLine($"{indent}{section.Key}:");
            foreach (var child in section.GetChildren())
            {
                PrintConfigurationValue(child, indent + "  ");
            }
        }
        else
        {
            var value = section.Value;

            // Mask sensitive values
            if (IsSensitiveKey(section.Key))
            {
                value = value != null ? "***CONFIGURED***" : "NOT SET";
            }

            Console.WriteLine($"{indent}{section.Key}: {value ?? "null"}");
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var sensitiveKeys = new[] { "apikey", "secret", "password", "token", "key" };
        return sensitiveKeys.Any(k => key.ToLowerInvariant().Contains(k));
    }

    private void ShowLlmConfiguration()
    {
        Console.WriteLine("LLM Provider Configuration:");
        Console.WriteLine("---------------------------");

        try
        {
            var llmOptions = _llmOptions.Value;
            Console.WriteLine($"Active Provider: {llmOptions.Provider}");
            Console.WriteLine($"Configuration Summary: {llmOptions.GetConfigurationSummary()}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error reading LLM configuration: {ex.Message}");
            Console.ResetColor();
        }
    }

    private void ShowMcpConfiguration()
    {
        Console.WriteLine("MCP Client Configuration:");
        Console.WriteLine("-------------------------");

        var mcpSection = _configuration.GetSection("McpClient");
        if (mcpSection.Exists())
        {
            Console.WriteLine($"Base URL: {mcpSection["BaseUrl"]}");
            Console.WriteLine($"Timeout: {mcpSection["TimeoutSeconds"]}s");
            Console.WriteLine($"Retry Attempts: {mcpSection["RetryAttempts"]}");
            Console.WriteLine($"Health Checks: {mcpSection["EnableHealthChecks"]}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("MCP configuration not found - using defaults");
            Console.ResetColor();
        }
    }

    private void ShowAgentConfiguration()
    {
        Console.WriteLine("Agent Configuration:");
        Console.WriteLine("--------------------");

        var agentSection = _configuration.GetSection("Agent");
        if (agentSection.Exists())
        {
            Console.WriteLine($"Working Directory: {agentSection["DefaultWorkingDirectory"]}");
            Console.WriteLine($"Execution Timeout: {agentSection["MaxExecutionTimeoutMinutes"]} minutes");
            Console.WriteLine($"Max Concurrent Tools: {agentSection["MaxConcurrentToolExecutions"]}");
            Console.WriteLine($"Context Persistence: {agentSection["EnableContextPersistence"]}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Agent configuration not found - using defaults");
            Console.ResetColor();
        }
    }

    private int ShowProviders()
    {
        Console.WriteLine("Available LLM Providers:");
        Console.WriteLine("========================");
        Console.WriteLine();

        var providers = new[]
        {
            ("OpenAI", "GPT-4, GPT-3.5-turbo models", "https://api.openai.com"),
            ("Anthropic", "Claude models", "https://api.anthropic.com"),
            ("Ollama", "Local models", "http://localhost:11434"),
            ("Azure OpenAI", "Azure-hosted OpenAI models", "Azure endpoint")
        };

        foreach (var (name, description, endpoint) in providers)
        {
            Console.WriteLine($"{name}:");
            Console.WriteLine($"  Description: {description}");
            Console.WriteLine($"  Endpoint: {endpoint}");
            Console.WriteLine();
        }

        Console.WriteLine("To configure a provider, update the appsettings.json file");
        Console.WriteLine("or use environment variables with the DEVMIND_ prefix.");

        return 0;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("DevMind Configuration Command");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  devmind config [subcommand]");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  validate          Validate current configuration");
        Console.WriteLine("  show [section]    Show configuration (optionally for specific section)");
        Console.WriteLine("  providers         Show available LLM providers");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  devmind config validate");
        Console.WriteLine("  devmind config show Llm");
        Console.WriteLine("  devmind config providers");
    }
}
