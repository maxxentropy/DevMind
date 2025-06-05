using DevMind.CLI.Commands;
using DevMind.CLI.Interfaces;
using DevMind.CLI.Services;
using DevMind.Core.Application.Interfaces;
using DevMind.Infrastructure.Configuration;
using DevMind.Infrastructure.McpClients;
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
        services.AddScoped<ConfigCommand>();    // Added
        services.AddScoped<LlmTestCommand>(); // Added
        services.AddScoped<StatusCommand>();    // Added

        // CLI Services
        // services.AddSingleton<IConsoleService, ConsoleService>(); // Added - Uncomment if IConsoleService is used

        // MCP Client Service
        services.AddScoped<IMcpClientService, HttpMcpClient>();
        services.Configure<McpClientOptions>(configuration.GetSection("McpClient"));

        // Infrastructure services (includes basic services but not LLM)
        services.AddInfrastructureServices(configuration);

        // LLM services
        services.AddLlmServices(configuration);

        return services;
    }
}
