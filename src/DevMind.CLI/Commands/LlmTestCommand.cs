// src/DevMind.CLI/Commands/LlmTestCommand.cs

using DevMind.CLI.Interfaces;
using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for testing LLM provider connectivity and the core reasoning loop.
/// </summary>
public class LlmTestCommand
{
    private readonly ILlmService _llmService;
    private readonly IConsoleService _console;
    private readonly ILogger<LlmTestCommand> _logger;

    public LlmTestCommand(
        ILlmService llmService,
        IConsoleService console,
        ILogger<LlmTestCommand> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        await _console.WriteBannerAsync("LLM Service Test");

        bool allTestsPassed = true;
        try
        {
            allTestsPassed &= await TestHealthCheckAsync();
            allTestsPassed &= await TestSimpleGenerationAsync();
            allTestsPassed &= await TestIntentAnalysisAsync();
            allTestsPassed &= await TestReasoningLoopAsync();

            await _console.WriteLineAsync();
            if (allTestsPassed)
            {
                await _console.WriteSuccessAsync("🎉 All LLM tests passed! Your LLM service is working correctly.");
                return 0;
            }
            else
            {
                await _console.WriteErrorAsync("❌ Some LLM tests failed. Please review the errors above.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred during the LLM test execution.");
            await _console.WriteErrorAsync($"A critical error occurred: {ex.Message}");
            return 1;
        }
    }

    private async Task<bool> TestHealthCheckAsync()
    {
        await _console.WriteLineAsync("1. Health Check", ConsoleColor.Yellow);
        var healthResult = await _llmService.HealthCheckAsync();
        if (healthResult.IsSuccess && healthResult.Value)
        {
            await _console.WriteInfoAsync("   Result: Service is healthy.");
            return true;
        }

        await _console.WriteLineAsync("   ❌ FAILED", ConsoleColor.Red);
        await _console.WriteErrorAsync($"   Error: {healthResult.Error?.Message ?? "Health check returned false."}");
        return false;
    }

    private async Task<bool> TestSimpleGenerationAsync()
    {
        await _console.WriteLineAsync("\n2. Simple Text Generation", ConsoleColor.Yellow);
        var generateResult = await _llmService.GenerateResponseAsync(
            "Say 'Hello, DevMind!' and nothing else.",
            new LlmOptions { MaxTokens = 20, Temperature = 0.1 });

        if (generateResult.IsSuccess && !string.IsNullOrWhiteSpace(generateResult.Value))
        {
            await _console.WriteInfoAsync($"   Response: {generateResult.Value.Trim()}");
            return true;
        }

        await _console.WriteLineAsync("   ❌ FAILED", ConsoleColor.Red);
        await _console.WriteErrorAsync($"   Error: {generateResult.Error?.Message ?? "No response was generated."}");
        return false;
    }

    private async Task<bool> TestIntentAnalysisAsync()
    {
        await _console.WriteLineAsync("\n3. Intent Analysis", ConsoleColor.Yellow);
        var request = UserRequest.Create("analyze my code for bugs");
        var intentResult = await _llmService.AnalyzeIntentAsync(request);

        if (intentResult.IsSuccess)
        {
            await _console.WriteInfoAsync($"   Intent: {intentResult.Value.Type}");
            await _console.WriteInfoAsync($"   Confidence: {intentResult.Value.Confidence}");
            return true;
        }

        await _console.WriteLineAsync("   ❌ FAILED", ConsoleColor.Red);
        await _console.WriteErrorAsync($"   Error: {intentResult.Error.Message}");
        return false;
    }

    private async Task<bool> TestReasoningLoopAsync()
    {
        await _console.WriteLineAsync("\n4. Multi-Step Reasoning Loop Simulation", ConsoleColor.Yellow);

        var history = new List<Result<ToolExecution>>();
        var availableTools = new List<ToolDefinition>
        {
            ToolDefinition.Create("list_plugins", "Lists available plugins and their IDs."),
            ToolDefinition.Create("validate_plugin", "Validates a specific plugin using its ID.")
        };

        // This is the corrected line
        var intent = UserIntent.Create("validate the CSharpCoveragePlugin", IntentType.SecurityScan);

        for (int i = 0; i < 3; i++) // Limit to a few steps for the test
        {
            await _console.WriteInfoAsync($"\n   Loop Iteration {i + 1}:");
            var nextStepResult = await _llmService.DetermineNextStepAsync(intent, availableTools, history);

            if (nextStepResult.IsFailure)
            {
                await _console.WriteLineAsync("   ❌ FAILED", ConsoleColor.Red);
                await _console.WriteErrorAsync($"   Error determining next step: {nextStepResult.Error.Message}");
                return false;
            }

            var toolCall = nextStepResult.Value;
            if (toolCall == null)
            {
                await _console.WriteInfoAsync("   LLM decided the task is complete.");
                return true; // Successfully finished the loop
            }

            await _console.WriteAsync($"   LLM decided to call: ", ConsoleColor.Cyan);
            await _console.WriteLineAsync($"{toolCall.ToolName} with params: {JsonSerializer.Serialize(toolCall.Parameters)}");

            // Simulate the execution of this tool to create an observation for the next loop
            var mockExecutionResult = CreateMockExecutionResult(toolCall);
            history.Add(mockExecutionResult);
            await _console.WriteAsync("   Simulating execution result: ", ConsoleColor.DarkGray);
            await _console.WriteLineAsync(mockExecutionResult.IsSuccess ? "Success" : "Failure", ConsoleColor.DarkGray);
        }

        return true;
    }

    /// <summary>
    /// Creates a mock tool execution result to simulate the "Act" step.
    /// </summary>
    private Result<ToolExecution> CreateMockExecutionResult(ToolCall toolCall)
    {
        if (toolCall.ToolName == "list_plugins")
        {
            var mockPluginList = new[]
            {
                new { id = "a1b2c3d4-e5f6-...", name = "CSharpCoveragePlugin", version = "1.0.0" },
                new { id = "f6e5d4c3-b2a1-...", name = "DependencyCheckPlugin", version = "1.2.0" }
            };
            return ToolExecution.Success(toolCall, mockPluginList);
        }

        if (toolCall.ToolName == "validate_plugin")
        {
            if (toolCall.Parameters.ContainsKey("pluginId"))
            {
                return ToolExecution.Success(toolCall, new { isValid = true, message = "Plugin is valid and configured correctly." });
            }
            else
            {
                return ToolExecution.Failure(toolCall, ToolErrorCodes.MissingRequiredParameter, "The 'pluginId' parameter is required for validation.");
            }
        }

        // Default success for any other tool
        return ToolExecution.Success(toolCall, "Mock execution successful.");
    }
}
