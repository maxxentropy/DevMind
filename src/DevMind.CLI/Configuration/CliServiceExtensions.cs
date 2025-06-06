using DevMind.CLI.Commands;
using DevMind.CLI.Interfaces;
using DevMind.CLI.Services;
using DevMind.Core.Application.Interfaces;
using DevMind.Infrastructure.Configuration;
using DevMind.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DevMind.CLI.Configuration;

/// <summary>
/// Extension methods for configuring CLI services in the dependency injection container.
/// Provides comprehensive setup for CLI commands, services, and infrastructure integration.
/// </summary>
public static class CliServiceExtensions
{
    /// <summary>
    /// Adds all CLI services and their dependencies to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddCliServices(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Core Application Architecture Registration ---

        // Register options classes to be read from appsettings.json
        services.Configure<AgentOptions>(configuration.GetSection("Agent"));

        // Register the new world-class services as singletons or scoped services.
        // PromptService and GuardrailService are stateless and can be scoped.
        services.AddScoped<IPromptService, PromptService>();
        services.AddScoped<IGuardrailService, GuardrailService>();

        // The In-Memory LTM must be a singleton to persist memory across commands during a single run.
        services.AddSingleton<ILongTermMemoryService, InMemoryLongTermMemoryService>();

        // --- Standard Service Registration ---

        // CLI command services
        services.AddCliCommands();

        // CLI utility services
        services.AddCliUtilityServices();

        // Infrastructure services (includes MCP client and core services)
        services.AddInfrastructureServices(configuration);

        // LLM services (includes all LLM providers and orchestration)
        services.AddLlmServices(configuration);

        // Configuration validation
        services.AddCliConfigurationValidation();

        // CLI-specific logging enhancements
        services.AddCliLogging();

        return services;
    }

    /// <summary>
    /// Adds all CLI command implementations.
    /// </summary>
    public static IServiceCollection AddCliCommands(this IServiceCollection services)
    {
        services.AddScoped<ProcessCommand>();
        services.AddScoped<TestCommand>();
        services.AddScoped<VersionCommand>();
        services.AddScoped<ConfigCommand>();
        services.AddScoped<StatusCommand>();
        services.AddScoped<LlmTestCommand>();
        services.AddSingleton<ICommandFactory, CommandFactory>();
        return services;
    }

    /// <summary>
    /// Adds CLI utility services.
    /// </summary>
    public static IServiceCollection AddCliUtilityServices(this IServiceCollection services)
    {
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddScoped<ICliErrorHandler, CliErrorHandler>();
        services.AddScoped<IUserExperienceService, UserExperienceService>();
        services.AddScoped<IProgressReporter, ConsoleProgressReporter>();
        return services;
    }

    /// <summary>
    /// Adds CLI-specific configuration validation.
    /// </summary>
    public static IServiceCollection AddCliConfigurationValidation(this IServiceCollection services)
    {
        services.AddHostedService<CliConfigurationValidator>();
        services.AddSingleton<ICliConfigurationValidator, ComprehensiveCliValidator>();
        return services;
    }

    /// <summary>
    /// Adds CLI-specific logging enhancements.
    /// </summary>
    public static IServiceCollection AddCliLogging(this IServiceCollection services)
    {
        services.AddSingleton<ICliLoggerProvider, CliLoggerProvider>();
        services.AddScoped<IUserFriendlyLogger, UserFriendlyLogger>();
        return services;
    }
}
