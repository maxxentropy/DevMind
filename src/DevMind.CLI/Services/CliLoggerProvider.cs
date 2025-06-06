using Microsoft.Extensions.Logging;

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
