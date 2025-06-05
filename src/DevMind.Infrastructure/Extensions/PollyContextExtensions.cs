// src/DevMind.Infrastructure/Extensions/PollyContextExtensions.cs

using Microsoft.Extensions.Logging;
using Polly;

namespace DevMind.Infrastructure.Extensions;

/// <summary>
/// Extension methods for Polly context
/// </summary>
public static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    /// <summary>
    /// Gets the logger from the Polly context
    /// </summary>
    /// <param name="context">The Polly context</param>
    /// <returns>The logger instance or null if not found</returns>
    public static ILogger? GetLogger(this Context context)
    {
        return context.TryGetValue(LoggerKey, out var logger) ? logger as ILogger : null;
    }

    /// <summary>
    /// Sets the logger in the Polly context
    /// </summary>
    /// <param name="context">The Polly context</param>
    /// <param name="logger">The logger to set</param>
    /// <returns>The context for chaining</returns>
    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }
}
