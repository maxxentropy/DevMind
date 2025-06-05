using DevMind.CLI.Commands;
using DevMind.CLI.Interfaces;
using DevMind.CLI.Services;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Configuration;

/// <summary>
/// Extension methods for configuring CLI services in the dependency injection container
/// Provides comprehensive setup for CLI commands, services, and infrastructure integration
/// </summary>
public static class CliServiceExtensions
{
    /// <summary>
    /// Adds all CLI services and their dependencies to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCliServices(this IServiceCollection services, IConfiguration configuration)
    {
        // CLI command services
        services.AddCliCommands();

        // CLI utility services
        services.AddCliUtilityServices();

        // Infrastructure services (includes MCP client and core services)
        services.AddInfrastructureServices(configuration);

        // LLM services (includes all LLM providers and orchestration)
        services.AddLlmServices(configuration);

        // Configuration validation
        services.AddCliConfigurationValidation();

        // CLI-specific logging enhancements
        services.AddCliLogging();

        return services;
    }

    /// <summary>
    /// Adds all CLI command implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCliCommands(this IServiceCollection services)
    {
        // Core command implementations
        services.AddScoped<ProcessCommand>();
        services.AddScoped<TestCommand>();
        services.AddScoped<VersionCommand>();

        // Configuration and diagnostics commands
        services.AddScoped<ConfigCommand>();
        services.AddScoped<StatusCommand>();

        // LLM-specific testing command
        services.AddScoped<LlmTestCommand>();

        // Command factory for dynamic command resolution
        services.AddSingleton<ICommandFactory, CommandFactory>();

        return services;
    }

    /// <summary>
    /// Adds CLI utility services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCliUtilityServices(this IServiceCollection services)
    {
        // Console interaction services
        services.AddSingleton<IConsoleService, ConsoleService>();

        // CLI-specific error handling and user experience
        services.AddScoped<ICliErrorHandler, CliErrorHandler>();
        services.AddScoped<IUserExperienceService, UserExperienceService>();

        // Progress reporting and user feedback
        services.AddScoped<IProgressReporter, ConsoleProgressReporter>();

        return services;
    }

    /// <summary>
    /// Adds CLI-specific configuration validation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCliConfigurationValidation(this IServiceCollection services)
    {
        // Add hosted service for startup validation
        services.AddHostedService<CliConfigurationValidator>();

        // Add individual validators
        services.AddSingleton<ICliConfigurationValidator, ComprehensiveCliValidator>();

        return services;
    }

    /// <summary>
    /// Adds CLI-specific logging enhancements
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCliLogging(this IServiceCollection services)
    {
        // Console logger enhancements for CLI
        services.AddSingleton<ICliLoggerProvider, CliLoggerProvider>();

        // User-friendly error reporting
        services.AddScoped<IUserFriendlyLogger, UserFriendlyLogger>();

        return services;
    }
}

#region Supporting Services and Interfaces

/// <summary>
/// Factory for creating command instances dynamically
/// </summary>
public interface ICommandFactory
{
    T CreateCommand<T>() where T : class;
    object? CreateCommand(string commandName);
    IEnumerable<string> GetAvailableCommands();
}

/// <summary>
/// Command factory implementation
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandFactory> _logger;

    private static readonly Dictionary<string, Type> CommandMap = new()
    {
        ["process"] = typeof(ProcessCommand),
        ["test"] = typeof(TestCommand),
        ["version"] = typeof(VersionCommand),
        ["config"] = typeof(ConfigCommand),
        ["status"] = typeof(StatusCommand),
        ["llm-test"] = typeof(LlmTestCommand)
    };

    public CommandFactory(IServiceProvider serviceProvider, ILogger<CommandFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public T CreateCommand<T>() where T : class
    {
        try
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create command of type {CommandType}", typeof(T).Name);
            throw;
        }
    }

    public object? CreateCommand(string commandName)
    {
        if (!CommandMap.TryGetValue(commandName.ToLowerInvariant(), out var commandType))
        {
            _logger.LogWarning("Unknown command requested: {CommandName}", commandName);
            return null;
        }

        try
        {
            return _serviceProvider.GetRequiredService(commandType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create command: {CommandName}", commandName);
            return null;
        }
    }

    public IEnumerable<string> GetAvailableCommands()
    {
        return CommandMap.Keys;
    }
}

/// <summary>
/// CLI-specific error handling service
/// </summary>
public interface ICliErrorHandler
{
    Task<int> HandleErrorAsync(Exception exception, string? context = null);
    Task<int> HandleValidationErrorsAsync(IEnumerable<string> errors, string? context = null);
    string FormatErrorForUser(Exception exception);
}

/// <summary>
/// CLI error handler implementation
/// </summary>
public class CliErrorHandler : ICliErrorHandler
{
    private readonly ILogger<CliErrorHandler> _logger;
    private readonly IConsoleService _console;

    public CliErrorHandler(ILogger<CliErrorHandler> logger, IConsoleService console)
    {
        _logger = logger;
        _console = console;
    }

    public async Task<int> HandleErrorAsync(Exception exception, string? context = null)
    {
        _logger.LogError(exception, "CLI error in context: {Context}", context ?? "Unknown");

        var userMessage = FormatErrorForUser(exception);
        await _console.WriteErrorAsync(userMessage);

        return GetExitCodeForException(exception);
    }

    public async Task<int> HandleValidationErrorsAsync(IEnumerable<string> errors, string? context = null)
    {
        var errorList = errors.ToList();
        _logger.LogWarning("Validation errors in context {Context}: {Errors}",
            context ?? "Unknown", string.Join(", ", errorList));

        await _console.WriteErrorAsync("Configuration validation failed:");
        foreach (var error in errorList)
        {
            await _console.WriteErrorAsync($"  • {error}");
        }

        return 1; // Validation error exit code
    }

    public string FormatErrorForUser(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException => $"Operation failed: {exception.Message}",
            ArgumentException => $"Invalid input: {exception.Message}",
            TimeoutException => "The operation timed out. Please try again.",
            HttpRequestException => "Network connection failed. Please check your connection and try again.",
            UnauthorizedAccessException => "Access denied. Please check your credentials.",
            _ => $"An unexpected error occurred: {exception.Message}"
        };
    }

    private static int GetExitCodeForException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => 2,
            InvalidOperationException => 3,
            UnauthorizedAccessException => 4,
            TimeoutException => 5,
            HttpRequestException => 6,
            _ => 1
        };
    }
}

/// <summary>
/// User experience enhancement service
/// </summary>
public interface IUserExperienceService
{
    Task ShowWelcomeAsync();
    Task ShowHelpAsync(string? command = null);
    Task<bool> PromptForConfirmationAsync(string message);
    Task ShowSuccessAsync(string message);
    Task ShowWarningAsync(string message);
}

/// <summary>
/// User experience service implementation
/// </summary>
public class UserExperienceService : IUserExperienceService
{
    private readonly IConsoleService _console;

    public UserExperienceService(IConsoleService console)
    {
        _console = console;
    }

    public async Task ShowWelcomeAsync()
    {
        await _console.WriteLineAsync("Welcome to DevMind AI Development Agent", ConsoleColor.Cyan);
        await _console.WriteLineAsync("Type 'help' for available commands or start with a request.", ConsoleColor.Gray);
        await _console.WriteLineAsync();
    }

    public async Task ShowHelpAsync(string? command = null)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            await ShowGeneralHelpAsync();
        }
        else
        {
            await ShowCommandHelpAsync(command);
        }
    }

    private async Task ShowGeneralHelpAsync()
    {
        await _console.WriteLineAsync("DevMind AI Development Agent", ConsoleColor.White);
        await _console.WriteLineAsync();
        await _console.WriteLineAsync("Usage:");
        await _console.WriteLineAsync("  devmind [command] [options]", ConsoleColor.Yellow);
        await _console.WriteLineAsync();
        await _console.WriteLineAsync("Commands:");
        await _console.WriteLineAsync("  test                 Test DevMind foundation and connections");
        await _console.WriteLineAsync("  version              Show version information");
        await _console.WriteLineAsync("  config               Show or modify configuration");
        await _console.WriteLineAsync("  status               Show system status and health");
        await _console.WriteLineAsync("  llm-test             Test LLM provider connections");
        await _console.WriteLineAsync("  help                 Show this help message");
        await _console.WriteLineAsync();
        await _console.WriteLineAsync("Examples:");
        await _console.WriteLineAsync("  devmind test", ConsoleColor.Green);
        await _console.WriteLineAsync("  devmind \"analyze my repository\"", ConsoleColor.Green);
        await _console.WriteLineAsync("  devmind status", ConsoleColor.Green);
    }

    private async Task ShowCommandHelpAsync(string command)
    {
        var helpText = command.ToLowerInvariant() switch
        {
            "test" => "Tests the DevMind foundation, including core services and integrations.",
            "version" => "Displays version information and build details.",
            "config" => "Shows current configuration or allows modification of settings.",
            "status" => "Displays system status, health checks, and service connectivity.",
            "llm-test" => "Tests connections to configured LLM providers.",
            _ => $"No specific help available for command '{command}'."
        };

        await _console.WriteLineAsync($"Help for '{command}':", ConsoleColor.White);
        await _console.WriteLineAsync(helpText);
    }

    public async Task<bool> PromptForConfirmationAsync(string message)
    {
        await _console.WriteAsync($"{message} (y/N): ", ConsoleColor.Yellow);
        var response = Console.ReadLine();
        return !string.IsNullOrWhiteSpace(response) &&
               response.Trim().ToLowerInvariant().StartsWith('y');
    }

    public async Task ShowSuccessAsync(string message)
    {
        await _console.WriteLineAsync($"✅ {message}", ConsoleColor.Green);
    }

    public async Task ShowWarningAsync(string message)
    {
        await _console.WriteLineAsync($"⚠️  {message}", ConsoleColor.Yellow);
    }
}

/// <summary>
/// Progress reporting service for CLI operations
/// </summary>
public interface IProgressReporter
{
    Task StartAsync(string operation);
    Task UpdateAsync(string status, int? percentage = null);
    Task CompleteAsync(string finalStatus);
    Task FailAsync(string error);
}

/// <summary>
/// Console-based progress reporter
/// </summary>
public class ConsoleProgressReporter : IProgressReporter
{
    private readonly IConsoleService _console;
    private string? _currentOperation;

    public ConsoleProgressReporter(IConsoleService console)
    {
        _console = console;
    }

    public async Task StartAsync(string operation)
    {
        _currentOperation = operation;
        await _console.WriteLineAsync($"Starting: {operation}...", ConsoleColor.Cyan);
    }

    public async Task UpdateAsync(string status, int? percentage = null)
    {
        var message = percentage.HasValue
            ? $"  {status} ({percentage}%)"
            : $"  {status}";
        await _console.WriteLineAsync(message, ConsoleColor.Gray);
    }

    public async Task CompleteAsync(string finalStatus)
    {
        await _console.WriteLineAsync($"✅ Completed: {finalStatus}", ConsoleColor.Green);
        _currentOperation = null;
    }

    public async Task FailAsync(string error)
    {
        await _console.WriteLineAsync($"❌ Failed: {error}", ConsoleColor.Red);
        _currentOperation = null;
    }
}

/// <summary>
/// CLI configuration validator service
/// </summary>
public interface ICliConfigurationValidator
{
    Task<List<string>> ValidateAllAsync();
    Task<List<string>> ValidateLlmConfigurationAsync();
    Task<List<string>> ValidateMcpConfigurationAsync();
    Task<List<string>> ValidateInfrastructureConfigurationAsync();
}

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

/// <summary>
/// CLI configuration validation hosted service
/// </summary>
public class CliConfigurationValidator : BackgroundService
{
    private readonly ICliConfigurationValidator _validator;
    private readonly ILogger<CliConfigurationValidator> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public CliConfigurationValidator(
        ICliConfigurationValidator validator,
        ILogger<CliConfigurationValidator> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _validator = validator;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting CLI configuration validation");

            var errors = await _validator.ValidateAllAsync();

            if (errors.Any())
            {
                _logger.LogError("CLI configuration validation failed with {ErrorCount} errors:", errors.Count);
                foreach (var error in errors)
                {
                    _logger.LogError("  - {Error}", error);
                }

                // For CLI applications, configuration errors should be non-fatal in most cases
                // Log the errors but allow the application to continue
                _logger.LogWarning("Continuing with invalid configuration - some features may not work correctly");
            }
            else
            {
                _logger.LogInformation("CLI configuration validation completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CLI configuration validation");
        }
    }
}

/// <summary>
/// CLI-specific logger provider
/// </summary>
public interface ICliLoggerProvider
{
    ILogger CreateLogger(string categoryName);
    void SetMinimumLevel(LogLevel level);
}

/// <summary>
/// CLI logger provider implementation
/// </summary>
public class CliLoggerProvider : ICliLoggerProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private LogLevel _minimumLevel = LogLevel.Information;

    public CliLoggerProvider(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }

    public void SetMinimumLevel(LogLevel level)
    {
        _minimumLevel = level;
    }
}

/// <summary>
/// User-friendly logging service for CLI
/// </summary>
public interface IUserFriendlyLogger
{
    Task LogSuccessAsync(string message);
    Task LogWarningAsync(string message);
    Task LogErrorAsync(string message);
    Task LogInfoAsync(string message);
    Task LogDebugAsync(string message);
}

/// <summary>
/// User-friendly logger implementation
/// </summary>
public class UserFriendlyLogger : IUserFriendlyLogger
{
    private readonly IConsoleService _console;
    private readonly ILogger<UserFriendlyLogger> _logger;

    public UserFriendlyLogger(IConsoleService console, ILogger<UserFriendlyLogger> logger)
    {
        _console = console;
        _logger = logger;
    }

    public async Task LogSuccessAsync(string message)
    {
        _logger.LogInformation("Success: {Message}", message);
        await _console.WriteLineAsync($"✅ {message}", ConsoleColor.Green);
    }

    public async Task LogWarningAsync(string message)
    {
        _logger.LogWarning("Warning: {Message}", message);
        await _console.WriteLineAsync($"⚠️  {message}", ConsoleColor.Yellow);
    }

    public async Task LogErrorAsync(string message)
    {
        _logger.LogError("Error: {Message}", message);
        await _console.WriteLineAsync($"❌ {message}", ConsoleColor.Red);
    }

    public async Task LogInfoAsync(string message)
    {
        _logger.LogInformation("Info: {Message}", message);
        await _console.WriteLineAsync($"ℹ️  {message}", ConsoleColor.Cyan);
    }

    public async Task LogDebugAsync(string message)
    {
        _logger.LogDebug("Debug: {Message}", message);
        // Only show debug messages in verbose mode
        await _console.WriteLineAsync($"🔍 {message}", ConsoleColor.DarkGray);
    }
}

#endregion
