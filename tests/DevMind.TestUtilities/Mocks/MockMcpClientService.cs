// tests/DevMind.TestUtilities/Mocks/MockMcpClientService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.TestUtilities.Mocks;

/// <summary>
/// Mock implementation of IMcpClientService for testing purposes
/// </summary>
public class MockMcpClientService : IMcpClientService
{
    #region Private Fields

    private readonly List<ToolDefinition> _availableTools = new();
    private readonly Queue<Result<ToolExecution>> _executionResponses = new();
    private readonly Queue<Result<bool>> _healthCheckResponses = new();
    private readonly Dictionary<string, Func<ToolCall, Result<ToolExecution>>> _toolHandlers = new();

    #endregion

    #region Configuration Methods

    /// <summary>
    /// Adds a tool to the available tools list
    /// </summary>
    /// <param name="tool">The tool definition to add</param>
    public void AddAvailableTool(ToolDefinition tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _availableTools.Add(tool);
    }

    /// <summary>
    /// Configures the next execution response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupExecution(Result<ToolExecution> response)
    {
        _executionResponses.Enqueue(response);
    }

    /// <summary>
    /// Configures a handler for a specific tool
    /// </summary>
    /// <param name="toolName">The name of the tool</param>
    /// <param name="handler">The handler function</param>
    public void SetupToolHandler(string toolName, Func<ToolCall, Result<ToolExecution>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(handler);

        _toolHandlers[toolName] = handler;
    }

    /// <summary>
    /// Configures the next health check response
    /// </summary>
    /// <param name="response">The response to return</param>
    public void SetupHealthCheck(Result<bool> response)
    {
        _healthCheckResponses.Enqueue(response);
    }

    #endregion

    #region IMcpClientService Implementation

    public Task<Result<IEnumerable<ToolDefinition>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<IEnumerable<ToolDefinition>>.Success(_availableTools.AsEnumerable()));
    }

    public Task<Result<ToolExecution>> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        // Check for specific tool handler first
        if (_toolHandlers.TryGetValue(toolCall.ToolName, out var handler))
        {
            return Task.FromResult(handler(toolCall));
        }

        // Use queued responses
        if (_executionResponses.Count > 0)
        {
            return Task.FromResult(_executionResponses.Dequeue());
        }

        // Default successful response
        var execution = ToolExecution.Success(
            toolCall,
            $"Mock result for {toolCall.ToolName}",
            TimeSpan.FromMilliseconds(100));

        return Task.FromResult(execution);
    }

    public Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (_healthCheckResponses.Count > 0)
        {
            return Task.FromResult(_healthCheckResponses.Dequeue());
        }

        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<ToolDefinition>> GetToolDefinitionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var tool = _availableTools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

        if (tool != null)
        {
            return Task.FromResult(Result<ToolDefinition>.Success(tool));
        }

        return Task.FromResult(Result<ToolDefinition>.Failure(
            ToolErrorCodes.ToolNotFound,
            $"Tool '{toolName}' not found"));
    }

    public Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var results = new List<Result<ToolExecution>>();

        foreach (var toolCall in toolCalls)
        {
            var result = ExecuteToolAsync(toolCall, cancellationToken).Result;
            results.Add(result);

            // Stop on first failure
            if (result.IsFailure)
            {
                break;
            }
        }

        var executions = results.Where(r => r.IsSuccess).Select(r => r.Value);
        return Task.FromResult(Result<IEnumerable<ToolExecution>>.Success(executions));
    }

    public Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsParallelAsync(
        IEnumerable<ToolCall> toolCalls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        // For mock purposes, just execute sequentially
        return ExecuteToolsAsync(toolCalls, cancellationToken);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clears all configured responses and tools
    /// </summary>
    public void Reset()
    {
        _availableTools.Clear();
        _executionResponses.Clear();
        _healthCheckResponses.Clear();
        _toolHandlers.Clear();
    }

    /// <summary>
    /// Sets up default tools for testing
    /// </summary>
    public void SetupDefaultTools()
    {
        Reset();

        var analysisTool = ToolDefinition.Create(
            "code_analyzer",
            "Analyzes code for quality and issues",
            new Dictionary<string, ToolParameter>
            {
                ["code"] = ToolParameter.Create("string", "Code to analyze", true),
                ["language"] = ToolParameter.Create("string", "Programming language", false, "auto")
            },
            new List<string> { "analysis", "code" });

        var documentationTool = ToolDefinition.Create(
            "doc_generator",
            "Generates documentation for code",
            new Dictionary<string, ToolParameter>
            {
                ["code"] = ToolParameter.Create("string", "Code to document", true),
                ["format"] = ToolParameter.Create("string", "Documentation format", false, "markdown")
            },
            new List<string> { "documentation", "generation" });

        AddAvailableTool(analysisTool);
        AddAvailableTool(documentationTool);

        // Setup default handlers
        SetupToolHandler("code_analyzer", toolCall =>
            ToolExecution.Success(toolCall, "Analysis complete: No issues found", TimeSpan.FromMilliseconds(150)));

        SetupToolHandler("doc_generator", toolCall =>
            ToolExecution.Success(toolCall, "# Documentation\n\nGenerated documentation", TimeSpan.FromMilliseconds(200)));
    }

    /// <summary>
    /// Sets up a failing scenario for a specific tool
    /// </summary>
    /// <param name="toolName">The tool that should fail</param>
    /// <param name="errorCode">The error code</param>
    /// <param name="errorMessage">The error message</param>
    public void SetupToolFailure(string toolName, string errorCode = ToolErrorCodes.ExecutionFailed, string errorMessage = "Tool execution failed")
    {
        SetupToolHandler(toolName, toolCall =>
            ToolExecution.Failure(toolCall, errorCode, errorMessage, TimeSpan.FromMilliseconds(50)));
    }

    #endregion
}
