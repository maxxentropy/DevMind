using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// Mock MCP client for testing when no real MCP server is available
/// </summary>
public class MockMcpClient : IMcpClientService
{
    private readonly ILogger<MockMcpClient> _logger;

    public MockMcpClient(ILogger<MockMcpClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("MockMcpClient initialized - using mock tools for development");
    }

    public Task<Result<IEnumerable<ToolDefinition>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("MockMcpClient: Returning mock tools");

        var tools = new List<ToolDefinition>
        {
            ToolDefinition.Create(
                "code_analyzer",
                "Analyzes code for quality and potential issues",
                new Dictionary<string, ToolParameter>
                {
                    ["code"] = ToolParameter.Create("string", "Code to analyze", true),
                    ["language"] = ToolParameter.Create("string", "Programming language", false, "auto")
                },
                new List<string> { "analysis", "code" }),

            ToolDefinition.Create(
                "doc_generator",
                "Generates documentation for code",
                new Dictionary<string, ToolParameter>
                {
                    ["code"] = ToolParameter.Create("string", "Code to document", true),
                    ["format"] = ToolParameter.Create("string", "Documentation format", false, "markdown")
                },
                new List<string> { "documentation", "generation" }),

            ToolDefinition.Create(
                "test_runner",
                "Runs tests for a project",
                new Dictionary<string, ToolParameter>
                {
                    ["project_path"] = ToolParameter.Create("string", "Path to project", true),
                    ["test_filter"] = ToolParameter.Create("string", "Test filter pattern", false)
                },
                new List<string> { "testing", "validation" })
        };

        return Task.FromResult(Result<IEnumerable<ToolDefinition>>.Success(tools.AsEnumerable()));
    }

    public Task<Result<ToolExecution>> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        _logger.LogDebug("MockMcpClient: Executing mock tool {ToolName}", toolCall.ToolName);

        // Simulate some processing time
        var processingTime = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500));

        var result = toolCall.ToolName switch
        {
            "code_analyzer" => "Code analysis complete: No critical issues found. 2 minor style suggestions.",
            "doc_generator" => "# Documentation\n\nGenerated documentation for the provided code.\n\n## Summary\nThis code appears to be well-structured.",
            "test_runner" => "Test execution complete: 15 tests passed, 0 failed, 0 skipped.",
            _ => $"Mock execution result for {toolCall.ToolName}"
        };

        return Task.FromResult(ToolExecution.Success(toolCall, result, processingTime));
    }

    public Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("MockMcpClient: Health check - always healthy");
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<ToolDefinition>> GetToolDefinitionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var tools = GetAvailableToolsAsync(cancellationToken).Result.Value;
        var tool = tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

        if (tool != null)
        {
            return Task.FromResult(Result<ToolDefinition>.Success(tool));
        }

        return Task.FromResult(Result<ToolDefinition>.Failure(
            ToolErrorCodes.ToolNotFound,
            $"Mock tool '{toolName}' not found"));
    }

    public Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var results = new List<ToolExecution>();

        foreach (var toolCall in toolCalls)
        {
            var result = ExecuteToolAsync(toolCall, cancellationToken).Result;
            if (result.IsSuccess)
            {
                results.Add(result.Value);
            }
            else
            {
                // Stop on first failure
                break;
            }
        }

        return Task.FromResult(Result<IEnumerable<ToolExecution>>.Success(results.AsEnumerable()));
    }

    public Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsParallelAsync(
        IEnumerable<ToolCall> toolCalls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default)
    {
        // For mock, just execute sequentially
        return ExecuteToolsAsync(toolCalls, cancellationToken);
    }
}
