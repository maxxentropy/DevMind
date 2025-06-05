using DevMind.CLI.Commands;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevMind.CLI.Configuration;

public static class CliServiceExtensions
{
    public static IServiceCollection AddCliServices(this IServiceCollection services, IConfiguration configuration)
    {
        // CLI Commands
        services.AddScoped<ProcessCommand>();
        services.AddScoped<TestCommand>();
        services.AddScoped<VersionCommand>();
        
        // Infrastructure services
        services.AddInfrastructureServices(configuration);
        
        return services;
    }
}
