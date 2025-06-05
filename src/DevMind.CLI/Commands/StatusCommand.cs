using DevMind.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for checking the overall status of DevMind services
/// </summary>
public class StatusCommand
{
    private readonly ILlmService _llmService;
    private readonly IMcpClientService _mcpClientService;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommand(
        ILlmService llmService,
        IMcpClientService mcpClientService,
        ILogger<StatusCommand> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            Console.WriteLine("DevMind Service Status Check");
            Console.WriteLine("============================");
            Console.WriteLine();

            var overallStatus = true;

            // Check LLM Service
            overallStatus &= await CheckLlmServiceStatus();
            Console.WriteLine();

            // Check MCP Client Service
            overallStatus &= await CheckMcpServiceStatus();
            Console.WriteLine();

            // Overall Status Summary
            Console.WriteLine("Overall Status:");
            Console.WriteLine("---------------");

            if (overallStatus)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ All services are operational");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("DevMind is ready to process your requests!");
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Some services are not operational");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Please check the errors above and fix any configuration issues.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Status check failed: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "Status command execution failed");
            return 1;
        }
    }

    private async Task<bool> CheckLlmServiceStatus()
    {
        Console.WriteLine("LLM Service:");
        Console.WriteLine("------------");

        try
        {
            Console.Write("Health Check... ");

            var healthResult = await _llmService.HealthCheckAsync();

            if (healthResult.IsSuccess && healthResult.Value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Healthy");
                Console.ResetColor();

                // Test basic functionality
                Console.Write("Basic Functionality... ");
                var testResult = await _llmService.GenerateResponseAsync(
                    "Test",
                    new Core.Domain.ValueObjects.LlmOptions { MaxTokens = 5, Temperature = 0.1 });

                if (testResult.IsSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Working");
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Failed");
                    Console.ResetColor();
                    Console.WriteLine($"   Error: {testResult.Error.Message}");
                    return false;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Unhealthy");
                Console.ResetColor();

                if (healthResult.IsFailure)
                {
                    Console.WriteLine($"   Error: {healthResult.Error.Message}");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error");
            Console.ResetColor();
            Console.WriteLine($"   Exception: {ex.Message}");

            _logger.LogWarning(ex, "LLM service health check failed");
            return false;
        }
    }

    private async Task<bool> CheckMcpServiceStatus()
    {
        Console.WriteLine("MCP Client Service:");
        Console.WriteLine("-------------------");

        try
        {
            Console.Write("Health Check... ");

            var healthResult = await _mcpClientService.HealthCheckAsync();

            if (healthResult.IsSuccess && healthResult.Value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Healthy");
                Console.ResetColor();

                // Test tool availability
                Console.Write("Tool Discovery... ");
                var toolsResult = await _mcpClientService.GetAvailableToolsAsync();

                if (toolsResult.IsSuccess)
                {
                    var toolCount = toolsResult.Value.Count();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ {toolCount} tools available");
                    Console.ResetColor();

                    if (toolCount > 0)
                    {
                        var firstTool = toolsResult.Value.First();
                        Console.WriteLine($"   Example tool: {firstTool.Name}");
                    }

                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No tools available");
                    Console.ResetColor();
                    Console.WriteLine($"   Warning: {toolsResult.Error.Message}");
                    return true; // MCP service is healthy, just no tools
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Unhealthy");
                Console.ResetColor();

                if (healthResult.IsFailure)
                {
                    Console.WriteLine($"   Error: {healthResult.Error.Message}");
                }
                else
                {
                    Console.WriteLine("   MCP server is not responding");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error");
            Console.ResetColor();
            Console.WriteLine($"   Exception: {ex.Message}");

            _logger.LogWarning(ex, "MCP service health check failed");
            return false;
        }
    }
}
