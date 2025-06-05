using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI.Commands;

public class TestCommand
{
    private readonly IAgentOrchestrationService _agentService;
    private readonly ILogger<TestCommand> _logger;

    public TestCommand(
        IAgentOrchestrationService agentService,
        ILogger<TestCommand> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            Console.WriteLine("Testing DevMind foundation...");

            // Test basic domain models
            Console.Write("Creating test user request... ");
            var request = UserRequest.Create("test basic functionality", Environment.CurrentDirectory);
            Console.WriteLine("✅");

            Console.Write("Creating test user intent... ");
            var intent = UserIntent.Create(request.Content, IntentType.AnalyzeCode);
            Console.WriteLine("✅");

            Console.Write("Creating test execution plan... ");
            var plan = ExecutionPlan.Create(intent);
            var toolCall = ToolCall.Create("test_tool", new Dictionary<string, object> { ["test"] = "value" });
            plan.AddStep(toolCall);
            Console.WriteLine("✅");

            Console.Write("Creating test tool definition... ");
            var toolDef = ToolDefinition.Create(
                "test_tool", 
                "A test tool for verification",
                new Dictionary<string, ToolParameter>
                {
                    ["test_param"] = ToolParameter.Create("string", "Test parameter", true)
                });
            Console.WriteLine("✅");

            Console.Write("Testing agent orchestration service... ");
            var response = await _agentService.ProcessUserRequestAsync(request);
            Console.WriteLine("✅");

            Console.WriteLine();
            Console.WriteLine("🎉 DevMind foundation is working!");
            Console.WriteLine($"Response: {response.Content}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            _logger.LogError(ex, "Test failed");
            return 1;
        }
    }
}
