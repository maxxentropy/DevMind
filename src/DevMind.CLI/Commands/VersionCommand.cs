// src/DevMind.CLI/Commands/VersionCommand.cs

using DevMind.CLI.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

public class VersionCommand
{
    private readonly IConsoleService _console;

    public VersionCommand(IConsoleService console)
    {
        _console = console;
    }

    public async Task<int> ExecuteAsync()
    {
        await _console.WriteBannerAsync("DevMind AI Development Agent");

        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);

        await _console.WriteKeyValueAsync("Version", $"{version.Major}.{version.Minor}.{version.Build}", 15, ConsoleColor.Yellow);
        await _console.WriteKeyValueAsync("Build Date", new DateTime(2000, 1, 1).AddDays(version.Build).ToShortDateString(), 15, ConsoleColor.Yellow);
        await _console.WriteKeyValueAsync(".NET Version", Environment.Version.ToString(), 15, ConsoleColor.Yellow);

        await _console.WriteLineAsync("\nCopyright (c) 2025, Sean Bennington. All rights reserved.", ConsoleColor.DarkGray);

        return 0;
    }
}
