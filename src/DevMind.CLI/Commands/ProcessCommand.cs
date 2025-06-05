using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Commands;

public class ProcessCommand
{
    private readonly IAgentOrchestrationService _agentService;
    private readonly ILogger<ProcessCommand> _logger;

    public ProcessCommand(
        IAgentOrchestrationService agentService,
        ILogger<ProcessCommand> logger)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            string? userInput;
            
            if (args.Length > 0)
            {
                // Command provided as arguments
                userInput = string.Join(" ", args);
            }
            else
            {
                // Interactive mode
                Console.Write("What would you like me to help you with? ");
                userInput = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("No input provided. Exiting.");
                    return 0;
                }
            }

            // Show processing indicator
            Console.WriteLine("Processing your request...");

            // Process the request
            var request = UserRequest.Create(userInput, Environment.CurrentDirectory);
            var response = await _agentService.ProcessUserRequestAsync(request);

            // Display the response
            if (response.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success:");
                Console.ResetColor();
                Console.WriteLine(response.Content);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error:");
                Console.ResetColor();
                Console.WriteLine(response.Content);
                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"Details: {response.Error}");
                }
            }

            return response.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user request");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
