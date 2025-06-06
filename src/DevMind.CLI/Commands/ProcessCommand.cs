// src/DevMind.CLI/Commands/ProcessCommand.cs

using DevMind.CLI.Interfaces;
using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for processing user requests through the DevMind agent system.
/// Handles both command-line arguments and interactive mode input.
/// </summary>
public class ProcessCommand
{
    #region Private Fields

    private readonly IAgentOrchestrationService _agentService;
    private readonly IConsoleService _console;
    private readonly ILogger<ProcessCommand> _logger;

    #endregion

    #region Constructor

    public ProcessCommand(
        IAgentOrchestrationService agentService,
        IConsoleService console,
        ILogger<ProcessCommand> logger)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var userInput = await GetUserInputAsync(args);
            if (string.IsNullOrWhiteSpace(userInput))
            {
                await _console.WriteWarningAsync("No input provided. Exiting.");
                return 0;
            }

            await _console.WriteLineAsync("Processing your request...", ConsoleColor.Cyan);

            var request = UserRequest.Create(userInput, Environment.CurrentDirectory);
            var result = await _agentService.ProcessUserRequestAsync(request);

            return await DisplayResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user request");
            await _console.WriteErrorAsync($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<string?> GetUserInputAsync(string[] args)
    {
        if (args.Length > 0)
        {
            return string.Join(" ", args);
        }

        // Interactive mode
        return await _console.ReadLineAsync("What would you like me to help you with? ");
    }

    private async Task<int> DisplayResultAsync(Result<AgentResponse> result)
    {
        await _console.WriteLineAsync(); // Add spacing

        if (result.IsSuccess)
        {
            var response = result.Value;
            switch (response.Type)
            {
                case ResponseType.Success:
                    await _console.WriteSuccessAsync("Request completed successfully.");
                    break;
                case ResponseType.Warning:
                    await _console.WriteWarningAsync("Request completed with warnings.");
                    break;
                case ResponseType.Clarification:
                    await _console.WriteInfoAsync("I need some clarification.");
                    break;
            }

            await _console.WriteBoxAsync(response.Content);
            await DisplayMetadataAsync(response);
            return 0;
        }
        else
        {
            await _console.WriteErrorAsync("Request processing failed:");
            await _console.WriteLineAsync(result.Error.Message);
            await DisplayErrorDetailsAsync(result.Error);
            return 1;
        }
    }

    private async Task DisplayMetadataAsync(AgentResponse response)
    {
        if (response.Metadata.Any())
        {
            await _console.WriteLineAsync("\nAdditional Information:", ConsoleColor.Gray);
            foreach (var metadata in response.Metadata.Take(5))
            {
                await _console.WriteKeyValueAsync(
                    FormatMetadataKey(metadata.Key),
                    FormatMetadataValue(metadata.Value),
                    keyWidth: 25,
                    keyColor: ConsoleColor.DarkGray
                );
            }

            if (response.Metadata.Count > 5)
            {
                await _console.WriteLineAsync($"... and {response.Metadata.Count - 5} more items", ConsoleColor.DarkGray);
            }
        }
    }

    private async Task DisplayErrorDetailsAsync(ResultError error)
    {
        if (!string.IsNullOrWhiteSpace(error.Code))
        {
            await _console.WriteKeyValueAsync("Error Code", error.Code, keyColor: ConsoleColor.DarkRed, valueColor: ConsoleColor.DarkGray);
        }
        if (error.Details != null)
        {
            await _console.WriteKeyValueAsync("Details", error.Details, keyColor: ConsoleColor.DarkRed, valueColor: ConsoleColor.DarkGray);
        }
    }

    private static string FormatMetadataKey(string key)
    {
        return string.Join(" ", key.Split('_').Select(word => char.ToUpper(word[0]) + word.Substring(1)));
    }

    private static string FormatMetadataValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToString("o"),
            TimeSpan ts => $"{ts.TotalSeconds:F2}s",
            _ => value?.ToString() ?? "N/A"
        };
    }

    #endregion
}
