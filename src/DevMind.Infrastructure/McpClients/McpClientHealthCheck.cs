using DevMind.Core.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// Health check for MCP client connectivity
/// </summary>
public class McpClientHealthCheck : IHealthCheck
{
    private readonly IMcpClientService _mcpClient;
    private readonly ILogger<McpClientHealthCheck> _logger;

    public McpClientHealthCheck(IMcpClientService mcpClient, ILogger<McpClientHealthCheck> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthResult = await _mcpClient.HealthCheckAsync(cancellationToken);

            if (healthResult.IsSuccess && healthResult.Value)
            {
                return HealthCheckResult.Healthy("MCP client is healthy");
            }

            return HealthCheckResult.Unhealthy("MCP client health check failed",
                healthResult.IsFailure ? new Exception(healthResult.Error.Message) : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP client health check threw exception");
            return HealthCheckResult.Unhealthy("MCP client health check threw exception", ex);
        }
    }
}

/// <summary>
/// Health check for agent orchestration service
/// </summary>
public class AgentOrchestrationHealthCheck : IHealthCheck
{
    private readonly IAgentOrchestrationService _orchestrationService;
    private readonly ILogger<AgentOrchestrationHealthCheck> _logger;

    public AgentOrchestrationHealthCheck(
        IAgentOrchestrationService orchestrationService,
        ILogger<AgentOrchestrationHealthCheck> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a basic health check by getting session history
            // This verifies the service is responsive without side effects
            var result = await _orchestrationService.GetSessionHistoryAsync(1, cancellationToken);

            if (result.IsSuccess)
            {
                return HealthCheckResult.Healthy("Agent orchestration service is healthy");
            }

            return HealthCheckResult.Degraded("Agent orchestration service returned error",
                new Exception(result.Error.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent orchestration health check threw exception");
            return HealthCheckResult.Unhealthy("Agent orchestration service health check failed", ex);
        }
    }
}
