using DevMind.CLI.Commands;
using DevMind.CLI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

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
