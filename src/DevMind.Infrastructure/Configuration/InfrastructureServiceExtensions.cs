using DevMind.Core.Application.Interfaces;
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
        
        // HTTP clients
        services.AddHttpClient();
        
        return services;
    }
}
