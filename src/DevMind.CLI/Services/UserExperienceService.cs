using DevMind.CLI.Interfaces;

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
