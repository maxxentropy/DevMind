// src/DevMind.CLI/Commands/TestCommand.cs

using DevMind.CLI.Interfaces;
using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

public class TestCommand
{
    private readonly IAgentOrchestrationService _agentService;
    private readonly IConsoleService _console;
    private readonly ILogger<TestCommand> _logger;

    public TestCommand(
        IAgentOrchestrationService agentService,
        IConsoleService console,
        ILogger<TestCommand> logger)
    {
        _agentService = agentService;
        _console = console;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync()
    {
        await _console.WriteBannerAsync("DevMind Foundation Test");

        try
        {
            await _console.WriteLineAsync("This test verifies the agent's core orchestration pipeline using mock services.");
            await _console.WriteLineAsync("It ensures all internal components are wired correctly.", ConsoleColor.Gray);
            await _console.WriteLineAsync();

            await _console.WriteAsync("🚀 Processing a test request through the agent... ");

            var request = UserRequest.Create("Run a foundational test of the system.", Guid.NewGuid().ToString());
            var result = await _agentService.ProcessUserRequestAsync(request);

            if (result.IsSuccess)
            {
                await _console.WriteLineAsync("✅ PASSED", ConsoleColor.Green);
                await _console.WriteSuccessAsync("Agent orchestration pipeline completed successfully.");
                await _console.WriteLineAsync("Agent Response:", ConsoleColor.Yellow);
                await _console.WriteBoxAsync(result.Value.Content, contentColor: ConsoleColor.Cyan);
                return 0;
            }
            else
            {
                await _console.WriteLineAsync("❌ FAILED", ConsoleColor.Red);
                await _console.WriteErrorAsync($"Agent orchestration failed: {result.Error.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Foundation test failed with a critical exception.");
            await _console.WriteErrorAsync($"Test failed: {ex.Message}");
            return 1;
        }
    }
}
