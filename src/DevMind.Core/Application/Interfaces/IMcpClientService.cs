using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Interface for MCP (Model Context Protocol) client operations using the Response pattern
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Gets all available tools from the MCP server
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing available tools or error information</returns>
    Task<Result<IEnumerable<ToolDefinition>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a tool call through the MCP server
    /// </summary>
    /// <param name="toolCall">The tool call to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the tool execution or error information</returns>
    Task<Result<ToolExecution>> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the MCP server
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether the server is healthy</returns>
    Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific tool
    /// </summary>
    /// <param name="toolName">The name of the tool to get information for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing tool definition or error information</returns>
    Task<Result<ToolDefinition>> GetToolDefinitionAsync(string toolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple tool calls in sequence
    /// </summary>
    /// <param name="toolCalls">The tool calls to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing all tool executions or error information</returns>
    Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple tool calls in parallel
    /// </summary>
    /// <param name="toolCalls">The tool calls to execute</param>
    /// <param name="maxConcurrency">Maximum number of concurrent executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing all tool executions or error information</returns>
    Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsParallelAsync(
        IEnumerable<ToolCall> toolCalls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default);
}
