using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Extensions;
using DevMind.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Diagnostics;

namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// HTTP implementation of the MCP client service using the Response pattern
/// </summary>
public class HttpMcpClient : IMcpClientService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<McpClientOptions> _options;
    private readonly ILogger<HttpMcpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region Constructor

    public HttpMcpClient(
        HttpClient httpClient,
        IOptions<McpClientOptions> options,
        ILogger<HttpMcpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #endregion

    #region IMcpClientService Implementation

    public async Task<Result<IEnumerable<ToolDefinition>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving available tools from MCP server");

            var response = await _httpClient.GetAsync("/api/tools", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<IEnumerable<ToolDefinition>>.Failure(
                    ToolErrorCodes.NetworkError,
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var mcpResponse = JsonSerializer.Deserialize<McpToolsResponse>(content, _jsonOptions);

            if (mcpResponse?.Tools == null)
            {
                return Result<IEnumerable<ToolDefinition>>.Failure(
                    ToolErrorCodes.DataFormatError,
                    "Invalid response format from MCP server");
            }

            var tools = mcpResponse.Tools.ToDomainModels();

            _logger.LogDebug("Retrieved {ToolCount} tools from MCP server", tools.Count());

            return Result<IEnumerable<ToolDefinition>>.Success(tools);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while retrieving tools");
            return Result<IEnumerable<ToolDefinition>>.Failure(
                ToolErrorCodes.NetworkError,
                $"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while retrieving tools");
            return Result<IEnumerable<ToolDefinition>>.Failure(
                ToolErrorCodes.ConnectionTimeout,
                "Request timeout while retrieving tools");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while retrieving tools");
            return Result<IEnumerable<ToolDefinition>>.Failure(
                ToolErrorCodes.DataFormatError,
                $"Invalid JSON response: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving tools");
            return Result<IEnumerable<ToolDefinition>>.Failure(
                ToolErrorCodes.ExecutionFailed,
                $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<ToolExecution>> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing tool: {ToolName}", toolCall.ToolName);

            var request = new McpToolRequest
            {
                Tool = toolCall.ToolName,
                Parameters = toolCall.Parameters,
                SessionId = toolCall.SessionId?.ToString()
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/tools/execute", requestContent, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return ToolExecution.Failure(
                    toolCall,
                    GetErrorCodeFromStatusCode(response.StatusCode),
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    stopwatch.Elapsed);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var mcpResponse = JsonSerializer.Deserialize<McpToolResponse>(responseContent, _jsonOptions);

            if (mcpResponse == null)
            {
                return ToolExecution.Failure(
                    toolCall,
                    ToolErrorCodes.DataFormatError,
                    "Invalid response format from MCP server",
                    stopwatch.Elapsed);
            }

            if (!mcpResponse.Success)
            {
                return ToolExecution.Failure(
                    toolCall,
                    ToolErrorCodes.ExecutionFailed,
                    mcpResponse.Error ?? "Tool execution failed",
                    stopwatch.Elapsed,
                    mcpResponse.Metadata);
            }

            _logger.LogDebug("Successfully executed tool: {ToolName} in {Duration}ms",
                toolCall.ToolName, stopwatch.ElapsedMilliseconds);

            return ToolExecution.Success(
                toolCall,
                mcpResponse.Result,
                stopwatch.Elapsed,
                mcpResponse.Metadata);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Network error while executing tool: {ToolName}", toolCall.ToolName);
            return ToolExecution.Failure(
                toolCall,
                ToolErrorCodes.NetworkError,
                $"Network error: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Timeout while executing tool: {ToolName}", toolCall.ToolName);
            return ToolExecution.Failure(
                toolCall,
                ToolErrorCodes.ExecutionTimeout,
                "Tool execution timeout",
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "JSON parsing error while executing tool: {ToolName}", toolCall.ToolName);
            return ToolExecution.Failure(
                toolCall,
                ToolErrorCodes.DataFormatError,
                $"Invalid JSON response: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error while executing tool: {ToolName}", toolCall.ToolName);
            return ToolExecution.FromException(toolCall, ex, stopwatch.Elapsed);
        }
    }

    public async Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing MCP server health check");

            var response = await _httpClient.GetAsync("/api/health", cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogDebug("MCP server health check result: {IsHealthy}", isHealthy);

            return Result<bool>.Success(isHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MCP server health check");
            return Result<bool>.Success(false); // Health check failures return false, not error
        }
    }

    public async Task<Result<ToolDefinition>> GetToolDefinitionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        try
        {
            _logger.LogDebug("Retrieving definition for tool: {ToolName}", toolName);

            var response = await _httpClient.GetAsync($"/api/tools/{Uri.EscapeDataString(toolName)}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<ToolDefinition>.Failure(
                    ToolErrorCodes.ToolNotFound,
                    $"Tool '{toolName}' not found");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<ToolDefinition>.Failure(
                    ToolErrorCodes.NetworkError,
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var mcpTool = JsonSerializer.Deserialize<McpToolDefinition>(content, _jsonOptions);

            if (mcpTool == null)
            {
                return Result<ToolDefinition>.Failure(
                    ToolErrorCodes.DataFormatError,
                    "Invalid tool definition format");
            }

            var toolDefinition = mcpTool.ToDomainModel();

            _logger.LogDebug("Retrieved definition for tool: {ToolName}", toolName);

            return Result<ToolDefinition>.Success(toolDefinition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tool definition: {ToolName}", toolName);
            return Result<ToolDefinition>.Failure(
                ToolErrorCodes.ExecutionFailed,
                $"Error retrieving tool definition: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var results = new List<Result<ToolExecution>>();

        foreach (var toolCall in toolCalls)
        {
            var result = await ExecuteToolAsync(toolCall, cancellationToken);
            results.Add(result);

            // Stop on first failure
            if (result.IsFailure)
            {
                _logger.LogWarning("Tool execution failed, stopping sequence: {ToolName} - {Error}",
                    toolCall.ToolName, result.Error.Message);
                break;
            }
        }

        var executions = results.Where(r => r.IsSuccess).Select(r => r.Value);
        return Result<IEnumerable<ToolExecution>>.Success(executions);
    }

    public async Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsParallelAsync(
        IEnumerable<ToolCall> toolCalls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        if (maxConcurrency <= 0)
        {
            return Result<IEnumerable<ToolExecution>>.Failure(
                ToolErrorCodes.InvalidParameters,
                "maxConcurrency must be greater than 0");
        }

        try
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = toolCalls.Select(async toolCall =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ExecuteToolAsync(toolCall, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            var executions = results.Where(r => r.IsSuccess).Select(r => r.Value);

            return Result<IEnumerable<ToolExecution>>.Success(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during parallel tool execution");
            return Result<IEnumerable<ToolExecution>>.Failure(
                ToolErrorCodes.ExecutionFailed,
                $"Parallel execution error: {ex.Message}");
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GetErrorCodeFromStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => ToolErrorCodes.InvalidParameters,
            System.Net.HttpStatusCode.Unauthorized => ToolErrorCodes.AuthenticationRequired,
            System.Net.HttpStatusCode.Forbidden => ToolErrorCodes.AccessDenied,
            System.Net.HttpStatusCode.NotFound => ToolErrorCodes.ToolNotFound,
            System.Net.HttpStatusCode.RequestTimeout => ToolErrorCodes.ExecutionTimeout,
            System.Net.HttpStatusCode.TooManyRequests => ToolErrorCodes.ConcurrencyLimitReached,
            System.Net.HttpStatusCode.InternalServerError => ToolErrorCodes.ServiceUnavailable,
            System.Net.HttpStatusCode.BadGateway => ToolErrorCodes.ServiceUnavailable,
            System.Net.HttpStatusCode.ServiceUnavailable => ToolErrorCodes.ServiceUnavailable,
            System.Net.HttpStatusCode.GatewayTimeout => ToolErrorCodes.ConnectionTimeout,
            _ => ToolErrorCodes.NetworkError
        };
    }

    #endregion
}
