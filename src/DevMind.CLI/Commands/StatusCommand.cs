// src/DevMind.CLI/Commands/StatusCommand.cs

using DevMind.CLI.Interfaces;
using DevMind.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for checking the overall status of DevMind services
/// </summary>
public class StatusCommand
{
    private readonly ILlmService _llmService;
    private readonly IMcpClientService _mcpClientService;
    private readonly IConsoleService _console;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommand(
        ILlmService llmService,
        IMcpClientService mcpClientService,
        IConsoleService console,
        ILogger<StatusCommand> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        await _console.WriteBannerAsync("DevMind Service Status");

        var llmOk = await CheckLlmServiceStatusAsync();
        var mcpOk = await CheckMcpServiceStatusAsync();

        await _console.WriteLineAsync();
        if (llmOk && mcpOk)
        {
            await _console.WriteSuccessAsync("All services are operational. DevMind is ready!");
            return 0;
        }

        await _console.WriteErrorAsync("One or more services are not operational. Please check the logs above.");
        return 1;
    }

    private async Task<bool> CheckServiceStatusAsync(string serviceName, Func<Task<(bool isSuccess, string message)>> checkFunc)
    {
        await _console.WriteAsync($"Checking {serviceName}... ");
        var (isSuccess, message) = await checkFunc();
        if (isSuccess)
        {
            await _console.WriteLineAsync("✅ ONLINE", ConsoleColor.Green);
            await _console.WriteLineAsync($"   - {message}", ConsoleColor.Gray);
            return true;
        }

        await _console.WriteLineAsync("❌ OFFLINE", ConsoleColor.Red);
        await _console.WriteLineAsync($"   - {message}", ConsoleColor.DarkYellow);
        return false;
    }

    private async Task<bool> CheckLlmServiceStatusAsync()
    {
        return await CheckServiceStatusAsync("LLM Service", async () =>
        {
            var healthResult = await _llmService.HealthCheckAsync();
            if (healthResult.IsFailure)
            {
                return (false, $"Health check failed: {healthResult.Error.Message}");
            }
            return (healthResult.Value, "Connection successful.");
        });
    }

    private async Task<bool> CheckMcpServiceStatusAsync()
    {
        return await CheckServiceStatusAsync("MCP Client Service", async () =>
        {
            var healthResult = await _mcpClientService.HealthCheckAsync();
            if (healthResult.IsFailure)
            {
                return (false, $"Health check failed: {healthResult.Error.Message}");
            }

            if (!healthResult.Value)
            {
                return (false, "Service is not responding to health checks.");
            }

            var toolsResult = await _mcpClientService.GetAvailableToolsAsync();
            if (toolsResult.IsFailure)
            {
                return (true, $"Service is online, but failed to get tools: {toolsResult.Error.Message}");
            }

            return (true, $"Connected successfully and found {toolsResult.Value.Count()} tools.");
        });
    }
}
