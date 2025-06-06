using Microsoft.Extensions.Logging;

/// <summary>
/// CLI-specific logger provider
/// </summary>
public interface ICliLoggerProvider
{
    ILogger CreateLogger(string categoryName);
    void SetMinimumLevel(LogLevel level);
}
