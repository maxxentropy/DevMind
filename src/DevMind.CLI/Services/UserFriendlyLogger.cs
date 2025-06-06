using DevMind.CLI.Interfaces;

using Microsoft.Extensions.Logging;

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
