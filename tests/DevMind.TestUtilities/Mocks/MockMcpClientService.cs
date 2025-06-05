using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.TestUtilities.Mocks;

public class MockMcpClientService : IMcpClientService
{
    // TODO: Implement class members
    
    public MockMcpClientService()
    {
        // TODO: Constructor implementation
    }

    public Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ToolDefinition>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
