using DevMind.CLI.Interfaces;

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
