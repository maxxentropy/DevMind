using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DevMind.Infrastructure.McpClients;

public class HttpMcpClient : IMcpClientService
{
    // TODO: Implement class members
    
    public HttpMcpClient()
    {
        // TODO: Constructor implementation
    }

    public Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Core.Domain.ValueObjects.ToolDefinition>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
