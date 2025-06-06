using DevMind.CLI.Interfaces;

using Microsoft.Extensions.Logging;

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
