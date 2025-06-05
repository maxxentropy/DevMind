using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Commands;

/// <summary>
/// Command for testing LLM provider connectivity and basic functionality
/// </summary>
public class LlmTestCommand
{
    private readonly ILlmService _llmService;
    private readonly ILogger<LlmTestCommand> _logger;

    public LlmTestCommand(
        ILlmService llmService,
        ILogger<LlmTestCommand> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            Console.WriteLine("Testing LLM Service Connection...");
            Console.WriteLine();

            // Test 1: Health Check
            Console.Write("1. Health Check... ");
            var healthResult = await _llmService.HealthCheckAsync();

            if (healthResult.IsSuccess && healthResult.Value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ PASSED");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ FAILED");
                Console.ResetColor();
                Console.WriteLine($"   Error: {healthResult.Error?.Message ?? "Health check returned false"}");
                return 1;
            }

            // Test 2: Simple Generation
            Console.Write("2. Simple Text Generation... ");
            var generateResult = await _llmService.GenerateResponseAsync(
                "Say 'Hello, DevMind!' and nothing else.",
                new LlmOptions { MaxTokens = 20, Temperature = 0.1 });

            if (generateResult.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ PASSED");
                Console.ResetColor();
                Console.WriteLine($"   Response: {generateResult.Value.Trim()}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ FAILED");
                Console.ResetColor();
                Console.WriteLine($"   Error: {generateResult.Error.Message}");
                return 1;
            }

            // Test 3: Intent Analysis
            Console.Write("3. Intent Analysis... ");
            var request = UserRequest.Create("analyze my code for bugs");
            var intentResult = await _llmService.AnalyzeIntentAsync(request);

            if (intentResult.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ PASSED");
                Console.ResetColor();
                Console.WriteLine($"   Intent: {intentResult.Value.Type}");
                Console.WriteLine($"   Confidence: {intentResult.Value.Confidence}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ FAILED");
                Console.ResetColor();
                Console.WriteLine($"   Error: {intentResult.Error.Message}");
                return 1;
            }

            // Test 4: Execution Plan Creation
            Console.Write("4. Execution Plan Creation... ");
            var tools = new List<ToolDefinition>
            {
                ToolDefinition.Create("code_analyzer", "Analyzes code for issues"),
                ToolDefinition.Create("bug_finder", "Finds potential bugs in code")
            };

            var planResult = await _llmService.CreateExecutionPlanAsync(intentResult.Value, tools);

            if (planResult.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ PASSED");
                Console.ResetColor();
                Console.WriteLine($"   Plan Steps: {planResult.Value.Steps.Count}");
                foreach (var step in planResult.Value.Steps.Take(3))
                {
                    Console.WriteLine($"   - {step.ToolName}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ FAILED");
                Console.ResetColor();
                Console.WriteLine($"   Error: {planResult.Error.Message}");
                return 1;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎉 All LLM tests passed! Your LLM service is working correctly.");
            Console.ResetColor();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "LLM test execution failed");
            return 1;
        }
    }
}
