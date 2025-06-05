using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface IMcpClientService
{
    Task<IEnumerable<ToolDefinition>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
    Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
