using DevMind.Core.Application.Interfaces;
using DevMind.Core.Application.Services;
using DevMind.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevMind.Infrastructure.Configuration;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core application services
        services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();

        // Or if using Enhanced version:
        // services.AddScoped<IAgentOrchestrationService, EnhancedAgentOrchestrationService>();

        services.AddScoped<IExecutionService, ToolExecutionService>(); // Added

        // services.AddScoped<ISessionManagementService, SessionManagementService>(); // Added - If ISessionManagementService is created and used

        // HTTP clients
        services.AddHttpClient();

        return services;
    }
}
