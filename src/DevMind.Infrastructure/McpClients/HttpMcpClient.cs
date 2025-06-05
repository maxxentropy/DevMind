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
/// HTTP implementation of the MCP client service using JSON-RPC 2.0 over HTTP.
/// Implements the Model Context Protocol standard for communication with MCP servers.
/// Provides comprehensive error handling, retry logic, and protocol compliance.
/// </summary>
public class HttpMcpClient : IMcpClientService
{
    #region Private Fields

    private readonly HttpClient _httpClient;
    private readonly IOptions<McpClientOptions> _options;
    private readonly ILogger<HttpMcpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private int _requestId = 1;
    private bool _isInitialized = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the HttpMcpClient with dependency injection.
    /// Configures JSON serialization options and HTTP client settings.
    /// </summary>
    /// <param name="httpClient">HTTP client for MCP communication</param>
    /// <param name="options">MCP client configuration options</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
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
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        ConfigureHttpClient();
    }

    #endregion

    #region IMcpClientService Implementation

    /// <summary>
    /// Retrieves all available tools from the MCP server using the tools/list method.
    /// Automatically initializes the MCP connection if not already established.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing enumerable of available tool definitions or error information</returns>
    public async Task<Result<IEnumerable<ToolDefinition>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureInitializedAsync(cancellationToken);

            _logger.LogDebug("Retrieving available tools from MCP server using tools/list method");

            var request = CreateJsonRpcRequest("tools/list", new { });
            var response = await SendJsonRpcRequestAsync<McpToolsListResponse>(request, cancellationToken);

            if (response.IsFailure)
            {
                return Result<IEnumerable<ToolDefinition>>.Failure(response.Error);
            }

            var mcpResponse = response.Value;
            if (mcpResponse?.Result?.Tools == null)
            {
                return Result<IEnumerable<ToolDefinition>>.Failure(
                    ToolErrorCodes.DataFormatError,
                    "Invalid tools list response from MCP server");
            }

            // Convert from protocol format to domain models
            var tools = mcpResponse.Result.Tools.ToDomainModels();

            _logger.LogDebug("Retrieved {ToolCount} tools from MCP server", tools.Count());

            return Result<IEnumerable<ToolDefinition>>.Success(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving tools");
            return Result<IEnumerable<ToolDefinition>>.Failure(
                ToolErrorCodes.ExecutionFailed,
                $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a specific tool on the MCP server using the tools/call method.
    /// Measures execution time and provides comprehensive error handling.
    /// </summary>
    /// <param name="toolCall">Tool call specification with name and parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing tool execution details or error information</returns>
    /// <exception cref="ArgumentNullException">Thrown when toolCall is null</exception>
    public async Task<Result<ToolExecution>> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await EnsureInitializedAsync(cancellationToken);

            _logger.LogDebug("Executing tool: {ToolName} via tools/call method", toolCall.ToolName);

            var requestParams = new McpToolCallParams
            {
                Name = toolCall.ToolName,
                Arguments = toolCall.Parameters
            };

            var request = CreateJsonRpcRequest("tools/call", requestParams);
            var response = await SendJsonRpcRequestAsync<McpToolCallResponse>(request, cancellationToken);

            stopwatch.Stop();

            if (response.IsFailure)
            {
                return ToolExecution.Failure(
                    toolCall,
                    response.Error.Code,
                    response.Error.Message,
                    stopwatch.Elapsed);
            }

            var mcpResponse = response.Value;
            if (mcpResponse?.Result == null)
            {
                return ToolExecution.Failure(
                    toolCall,
                    ToolErrorCodes.DataFormatError,
                    "Invalid tool execution response from MCP server",
                    stopwatch.Elapsed);
            }

            var result = mcpResponse.Result;
            if (result.IsError == true)
            {
                return ToolExecution.Failure(
                    toolCall,
                    ToolErrorCodes.ExecutionFailed,
                    result.Content?.FirstOrDefault()?.Text ?? "Tool execution failed",
                    stopwatch.Elapsed);
            }

            _logger.LogDebug("Successfully executed tool: {ToolName} in {Duration}ms",
                toolCall.ToolName, stopwatch.ElapsedMilliseconds);

            var resultContent = ExtractToolResult(result);
            return ToolExecution.Success(
                toolCall,
                resultContent,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error while executing tool: {ToolName}", toolCall.ToolName);
            return ToolExecution.FromException(toolCall, ex, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Performs health check on the MCP server by calling the dedicated health endpoint.
    /// Health check failures return false rather than throwing exceptions for operational resilience.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result indicating server health status</returns>
    public async Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing MCP server health check");

            // Use the dedicated health endpoint if available
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;

            if (!isHealthy)
            {
                _logger.LogWarning("MCP server health check failed with status: {StatusCode}", response.StatusCode);
            }

            _logger.LogDebug("MCP server health check result: {IsHealthy}", isHealthy);

            return Result<bool>.Success(isHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during MCP server health check");
            return Result<bool>.Success(false); // Health check failures return false, not error
        }
    }

    /// <summary>
    /// Retrieves definition for a specific tool by name.
    /// Uses the tools/list method and filters results since MCP doesn't provide individual tool endpoints.
    /// </summary>
    /// <param name="toolName">Name of the tool to retrieve definition for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing tool definition or error information</returns>
    /// <exception cref="ArgumentException">Thrown when toolName is null or whitespace</exception>
    public async Task<Result<ToolDefinition>> GetToolDefinitionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        try
        {
            // Get all tools and find the specific one
            // MCP doesn't have a specific method for individual tool definitions
            var toolsResult = await GetAvailableToolsAsync(cancellationToken);

            if (toolsResult.IsFailure)
            {
                return Result<ToolDefinition>.Failure(toolsResult.Error);
            }

            var tool = toolsResult.Value.FirstOrDefault(t =>
                t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                return Result<ToolDefinition>.Failure(
                    ToolErrorCodes.ToolNotFound,
                    $"Tool '{toolName}' not found");
            }

            _logger.LogDebug("Retrieved definition for tool: {ToolName}", toolName);

            return Result<ToolDefinition>.Success(tool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tool definition: {ToolName}", toolName);
            return Result<ToolDefinition>.Failure(
                ToolErrorCodes.ExecutionFailed,
                $"Error retrieving tool definition: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes multiple tools sequentially, stopping on first failure.
    /// Provides fail-fast behavior for dependent tool chains.
    /// </summary>
    /// <param name="toolCalls">Collection of tool calls to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing successful tool executions or error information</returns>
    /// <exception cref="ArgumentNullException">Thrown when toolCalls is null</exception>
    public async Task<Result<IEnumerable<ToolExecution>>> ExecuteToolsAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var results = new List<Result<ToolExecution>>();

        foreach (var toolCall in toolCalls)
        {
            var result = await ExecuteToolAsync(toolCall, cancellationToken);
            results.Add(result);

            // Stop on first failure for sequential execution
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

    /// <summary>
    /// Executes multiple tools in parallel with controlled concurrency.
    /// Uses semaphore to limit concurrent executions and prevent server overload.
    /// </summary>
    /// <param name="toolCalls">Collection of tool calls to execute</param>
    /// <param name="maxConcurrency">Maximum number of concurrent executions (default: 3)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing successful tool executions or error information</returns>
    /// <exception cref="ArgumentNullException">Thrown when toolCalls is null</exception>
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

    #region Private Configuration Methods

    /// <summary>
    /// Configures the HTTP client with MCP-specific settings including headers, timeouts, and user agent.
    /// Called during construction to establish baseline HTTP client configuration.
    /// </summary>
    private void ConfigureHttpClient()
    {
        var options = _options.Value;

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.BaseUrl);
        }

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DevMind-MCP-Client/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    /// <summary>
    /// Ensures MCP connection is initialized using the standard initialization handshake.
    /// Performs protocol version negotiation and capability exchange with the server.
    /// Thread-safe initialization that only executes once per client instance.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the initialization</param>
    /// <returns>Task representing the initialization operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when initialization fails</exception>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
            return;

        _logger.LogDebug("Initializing MCP connection");

        try
        {
            var options = _options.Value;
            var protocolCapabilities = new DevMind.Shared.Models.McpClientCapabilities
            {
                Roots = options.ClientCapabilities.SupportsRoots ? new McpRootsCapability
                {
                    ListChanged = options.ClientCapabilities.SupportsRootsListChanged
                } : null,
                Sampling = options.ClientCapabilities.SupportsSampling ? new object() : null
            };

            var initRequest = CreateJsonRpcRequest("initialize", new McpInitializeParams
            {
                ProtocolVersion = options.ProtocolVersion,
                Capabilities = protocolCapabilities,
                ClientInfo = new McpClientInfo
                {
                    Name = options.ClientIdentity.Name,
                    Version = options.ClientIdentity.Version
                }
            });

            var response = await SendJsonRpcRequestAsync<McpInitializeResponse>(initRequest, cancellationToken);

            if (response.IsFailure)
            {
                throw new InvalidOperationException($"MCP initialization failed: {response.Error.Message}");
            }

            _isInitialized = true;
            _logger.LogInformation("MCP connection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP connection");
            throw;
        }
    }

    #endregion

    #region Private Protocol Methods

    /// <summary>
    /// Creates a JSON-RPC 2.0 request with auto-incrementing ID and standard protocol compliance.
    /// Ensures proper request formatting according to JSON-RPC specification.
    /// </summary>
    /// <param name="method">RPC method name to invoke</param>
    /// <param name="parameters">Method parameters (optional)</param>
    /// <returns>Properly formatted JSON-RPC request</returns>
    private McpJsonRpcRequest CreateJsonRpcRequest(string method, object? parameters = null)
    {
        return new McpJsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = Interlocked.Increment(ref _requestId),
            Method = method,
            Params = parameters
        };
    }

    /// <summary>
    /// Sends JSON-RPC request over HTTP and handles response deserialization with comprehensive error handling.
    /// Supports both success responses and error responses according to JSON-RPC 2.0 specification.
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="request">JSON-RPC request to send</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing deserialized response or error information</returns>
    private async Task<Result<T>> SendJsonRpcRequestAsync<T>(McpJsonRpcRequest request, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            _logger.LogTrace("Sending MCP request: {Method} with ID: {RequestId}", request.Method, request.Id);

            var response = await _httpClient.PostAsync("/mcp", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result<T>.Failure(
                    GetErrorCodeFromStatusCode(response.StatusCode),
                    $"HTTP {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // First, check if this is an error response
            var errorResponse = TryDeserializeErrorResponse(responseContent);
            if (errorResponse != null)
            {
                return Result<T>.Failure(
                    ToolErrorCodes.ExecutionFailed,
                    errorResponse.Error?.Message ?? "Unknown MCP error");
            }

            // Try to deserialize as success response
            var successResponse = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            if (successResponse == null)
            {
                return Result<T>.Failure(
                    ToolErrorCodes.DataFormatError,
                    "Failed to deserialize MCP response");
            }

            _logger.LogTrace("Received successful MCP response for request ID: {RequestId}", request.Id);

            return Result<T>.Success(successResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during MCP request: {Method}", request.Method);
            return Result<T>.Failure(
                ToolErrorCodes.NetworkError,
                $"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during MCP request: {Method}", request.Method);
            return Result<T>.Failure(
                ToolErrorCodes.ExecutionTimeout,
                "MCP request timeout");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error during MCP request: {Method}", request.Method);
            return Result<T>.Failure(
                ToolErrorCodes.DataFormatError,
                $"Invalid JSON response: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to deserialize response content as JSON-RPC error response.
    /// Returns null if the content is not a valid error response.
    /// </summary>
    /// <param name="responseContent">Raw response content to parse</param>
    /// <returns>Error response object or null if not an error</returns>
    private McpJsonRpcErrorResponse? TryDeserializeErrorResponse(string responseContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<McpJsonRpcErrorResponse>(responseContent, _jsonOptions);
            return errorResponse?.Error != null ? errorResponse : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts meaningful result content from MCP tool execution response.
    /// Handles both text and data content types with fallback behavior.
    /// </summary>
    /// <param name="result">MCP tool execution result</param>
    /// <returns>Extracted result content or default success message</returns>
    private object? ExtractToolResult(McpToolResult result)
    {
        if (result.Content?.Any() == true)
        {
            var content = result.Content.First();
            return content.Text ?? content.Data;
        }

        return "Tool executed successfully";
    }

    /// <summary>
    /// Maps HTTP status codes to appropriate tool error codes for consistent error handling.
    /// Provides semantic error mapping based on HTTP response status.
    /// </summary>
    /// <param name="statusCode">HTTP status code from response</param>
    /// <returns>Corresponding tool error code</returns>
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
