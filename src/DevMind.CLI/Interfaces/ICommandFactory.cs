using System.Collections.Generic;

namespace DevMind.CLI.Interfaces;

/// <summary>
/// Factory for creating command instances dynamically
/// </summary>
public interface ICommandFactory
{
    T CreateCommand<T>() where T : class;
    object? CreateCommand(string commandName);
    IEnumerable<string> GetAvailableCommands();
}
