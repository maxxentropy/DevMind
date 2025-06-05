// src/DevMind.CLI/Commands/ProcessCommand.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for processing user requests through the DevMind agent system.
/// Handles both command-line arguments and interactive mode input.
/// </summary>
public class ProcessCommand
{
    #region Private Fields

    private readonly IAgentOrchestrationService _agentService;
    private readonly ILogger<ProcessCommand> _logger;

    #endregion

    #region Constructor

    public ProcessCommand(
        IAgentOrchestrationService agentService,
        ILogger<ProcessCommand> logger)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the process command with the provided arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code (0 for success, 1 for failure)</returns>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var userInput = GetUserInput(args);
            if (string.IsNullOrWhiteSpace(userInput))
            {
                DisplayMessage("No input provided. Exiting.", ConsoleColor.Yellow);
                return 0;
            }

            // Show processing indicator
            DisplayMessage("Processing your request...", ConsoleColor.Cyan);

            // Process the request
            var request = UserRequest.Create(userInput, Environment.CurrentDirectory);
            var result = await _agentService.ProcessUserRequestAsync(request);

            // Display the response
            var exitCode = DisplayResult(result);

            _logger.LogInformation("Process command completed with exit code: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user request");
            DisplayError($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets user input from command line arguments or interactive mode
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>User input string</returns>
    private static string? GetUserInput(string[] args)
    {
        if (args.Length > 0)
        {
            // Command provided as arguments
            return string.Join(" ", args);
        }

        // Interactive mode
        Console.Write("What would you like me to help you with? ");
        return Console.ReadLine();
    }

    /// <summary>
    /// Displays the result of the agent processing
    /// </summary>
    /// <param name="result">The result from the agent service</param>
    /// <returns>Exit code based on result success</returns>
    private int DisplayResult(Result<AgentResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = result.Value;

            // Determine display based on response type
            var (color, prefix) = GetDisplayFormat(response.Type);

            DisplayMessage($"{prefix}:", color);
            Console.WriteLine(response.Content);

            // Display metadata if present
            DisplayMetadata(response);

            return 0;
        }
        else
        {
            DisplayError("Request processing failed:");
            Console.WriteLine(result.Error.Message);

            // Display additional error details if available
            DisplayErrorDetails(result.Error);

            return 1;
        }
    }

    /// <summary>
    /// Gets the display format (color and prefix) based on response type
    /// </summary>
    /// <param name="responseType">The type of response</param>
    /// <returns>Tuple of console color and display prefix</returns>
    private static (ConsoleColor Color, string Prefix) GetDisplayFormat(ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Success => (ConsoleColor.Green, "Success"),
            ResponseType.Warning => (ConsoleColor.Yellow, "Warning"),
            ResponseType.Error => (ConsoleColor.Red, "Error"),
            ResponseType.Clarification => (ConsoleColor.Cyan, "Clarification"),
            ResponseType.Information => (ConsoleColor.White, "Information"),
            _ => (ConsoleColor.White, "Response")
        };
    }

    /// <summary>
    /// Displays response metadata if present
    /// </summary>
    /// <param name="response">The agent response</param>
    private static void DisplayMetadata(AgentResponse response)
    {
        if (response.Metadata.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Additional Information:");

            foreach (var metadata in response.Metadata.Take(5)) // Limit to avoid clutter
            {
                var value = FormatMetadataValue(metadata.Value);
                Console.WriteLine($"  {FormatMetadataKey(metadata.Key)}: {value}");
            }

            if (response.Metadata.Count > 5)
            {
                Console.WriteLine($"  ... and {response.Metadata.Count - 5} more items");
            }

            Console.ResetColor();
        }
    }

    /// <summary>
    /// Displays error details if available
    /// </summary>
    /// <param name="error">The error information</param>
    private static void DisplayErrorDetails(ResultError error)
    {
        // Display error code if present
        if (!string.IsNullOrWhiteSpace(error.Code))
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Error Code: {error.Code}");
            Console.ResetColor();
        }

        // Display error details if present
        if (error.Details != null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Details: {error.Details}");
            Console.ResetColor();
        }

        // Display relevant metadata
        if (error.Metadata.Any())
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Technical Details:");

            foreach (var metadata in error.Metadata.Take(3))
            {
                var value = FormatMetadataValue(metadata.Value);
                Console.WriteLine($"  {FormatMetadataKey(metadata.Key)}: {value}");
            }

            Console.ResetColor();
        }
    }

    /// <summary>
    /// Displays a message with the specified color
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="color">Console color to use</param>
    private static void DisplayMessage(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Displays an error message
    /// </summary>
    /// <param name="message">Error message to display</param>
    private static void DisplayError(string message)
    {
        DisplayMessage(message, ConsoleColor.Red);
    }

    /// <summary>
    /// Formats a metadata key for display
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <returns>Formatted key</returns>
    private static string FormatMetadataKey(string key)
    {
        // Convert snake_case or camelCase to Title Case
        return string.Join(" ",
            key.Split('_', StringSplitOptions.RemoveEmptyEntries)
               .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
    }

    /// <summary>
    /// Formats a metadata value for display
    /// </summary>
    /// <param name="value">The metadata value</param>
    /// <returns>Formatted value</returns>
    private static string FormatMetadataValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpan ts => $"{ts.TotalSeconds:F1}s",
            decimal d => d.ToString("F2"),
            double d => d.ToString("F2"),
            float f => f.ToString("F2"),
            bool b => b ? "Yes" : "No",
            Guid g => g.ToString("D")[..8] + "...", // Show first 8 characters
            null => "N/A",
            _ => value.ToString() ?? "N/A"
        };
    }

    #endregion
}
