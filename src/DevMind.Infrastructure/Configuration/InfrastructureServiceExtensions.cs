using DevMind.Core.Application.Interfaces;
using DevMind.Core.Application.Services;
using DevMind.Infrastructure.McpClients;
using DevMind.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevMind.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring infrastructure services in the dependency injection container
/// Provides comprehensive setup for core application services, MCP clients, and HTTP configurations
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds all infrastructure services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core application services
        services.AddCoreApplicationServices();

        // MCP client services
        services.AddMcpClientServices(configuration);

        // Agent orchestration services
        services.AddAgentOrchestrationServices();

        // HTTP clients with policies
        services.AddInfrastructureHttpClients(configuration);

        // Health checks
        services.AddInfrastructureHealthChecks();

        return services;
    }

    /// <summary>
    /// Adds core application services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreApplicationServices(this IServiceCollection services)
    {
        // Core domain services
        services.AddScoped<IIntentAnalysisService, IntentAnalysisService>();
        services.AddScoped<IPlanningService, TaskPlanningService>();
        services.AddScoped<ISynthesisService, ResponseSynthesisService>();
        services.AddScoped<IExecutionService, ToolExecutionService>();

        return services;
    }

    /// <summary>
    /// Adds MCP client services with proper configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MCP client options
        services.Configure<McpClientOptions>(configuration.GetSection("McpClient"));

        // Add options validation
        services.AddSingleton<IValidateOptions<McpClientOptions>, McpClientOptionsValidator>();

        // Register MCP client service
        services.AddScoped<IMcpClientService, HttpMcpClient>();

        // Add MCP-specific HTTP client
        services.AddHttpClient<HttpMcpClient>("McpClient", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<McpClientOptions>>().Value;
            ConfigureMcpHttpClient(client, options);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        .AddPolicyHandler(GetMcpRetryPolicy())
        .AddPolicyHandler(GetMcpTimeoutPolicy());

        return services;
    }

    /// <summary>
    /// Adds agent orchestration services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentOrchestrationServices(this IServiceCollection services)
    {
        // Choose between standard and enhanced orchestration service
        services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();

        // Alternative: Enhanced version with additional features
        // services.AddScoped<IAgentOrchestrationService, EnhancedAgentOrchestrationService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure-related HTTP clients
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Default HTTP client for general infrastructure needs
        services.AddHttpClient("Infrastructure", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DevMind-Infrastructure/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetStandardRetryPolicy())
        .AddPolicyHandler(GetStandardTimeoutPolicy());

        return services;
    }

    /// <summary>
    /// Adds health checks for infrastructure services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<McpClientHealthCheck>("mcp-client")
            .AddCheck<AgentOrchestrationHealthCheck>("agent-orchestration");

        return services;
    }

    #region Private Configuration Methods

    /// <summary>
    /// Configures HTTP client specifically for MCP communication
    /// </summary>
    /// <param name="client">HTTP client to configure</param>
    /// <param name="options">MCP client options</param>
    private static void ConfigureMcpHttpClient(HttpClient client, McpClientOptions options)
    {
        // Set base address
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl);
        }

        // Set timeout
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        // Set standard headers
        client.DefaultRequestHeaders.Add("User-Agent", $"{options.ClientIdentity.Name}/{options.ClientIdentity.Version}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Configure authentication
        ConfigureAuthentication(client, options.Authentication);

        // Add custom headers
        foreach (var (key, value) in options.CustomHeaders)
        {
            client.DefaultRequestHeaders.Add(key, value);
        }

        // Configure compression
        if (options.Compression.EnableRequestCompression)
        {
            // Request compression would be handled by message handlers
        }
    }

    /// <summary>
    /// Configures authentication for HTTP client
    /// </summary>
    /// <param name="client">HTTP client to configure</param>
    /// <param name="authOptions">Authentication options</param>
    private static void ConfigureAuthentication(HttpClient client, McpAuthenticationOptions authOptions)
    {
        switch (authOptions.AuthType)
        {
            case McpAuthType.ApiKey when !string.IsNullOrWhiteSpace(authOptions.ApiKey):
                client.DefaultRequestHeaders.Add("X-API-Key", authOptions.ApiKey);
                break;

            case McpAuthType.Bearer when !string.IsNullOrWhiteSpace(authOptions.BearerToken):
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authOptions.BearerToken);
                break;

            case McpAuthType.Basic when !string.IsNullOrWhiteSpace(authOptions.Username) && !string.IsNullOrWhiteSpace(authOptions.Password):
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{authOptions.Username}:{authOptions.Password}"));
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                break;

            case McpAuthType.Custom:
                foreach (var (key, value) in authOptions.CustomHeaders)
                {
                    client.DefaultRequestHeaders.Add(key, value);
                }
                break;
        }
    }

    #endregion

    #region Polly Policies

    /// <summary>
    /// Creates retry policy specifically for MCP client
    /// </summary>
    /// <returns>Retry policy for MCP operations</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetMcpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode && ShouldRetryMcpRequest(msg.StatusCode))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    if (outcome.Exception != null)
                    {
                        logger?.LogWarning("MCP retry {RetryCount} after {Delay}ms due to {Exception}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception.GetType().Name);
                    }
                    else
                    {
                        logger?.LogWarning("MCP retry {RetryCount} after {Delay}ms due to {StatusCode}",
                            retryCount, timespan.TotalMilliseconds, outcome.Result?.StatusCode);
                    }
                });
    }

    /// <summary>
    /// Creates timeout policy for MCP client
    /// </summary>
    /// <returns>Timeout policy for MCP operations</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetMcpTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(60),
            timeoutStrategy: Polly.Timeout.TimeoutStrategy.Pessimistic);
    }

    /// <summary>
    /// Creates standard retry policy for infrastructure HTTP clients
    /// </summary>
    /// <returns>Standard retry policy</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetStandardRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Infrastructure HTTP retry {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates standard timeout policy for infrastructure HTTP clients
    /// </summary>
    /// <returns>Standard timeout policy</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetStandardTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(30),
            timeoutStrategy: Polly.Timeout.TimeoutStrategy.Pessimistic);
    }

    /// <summary>
    /// Determines if an MCP request should be retried based on status code
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>True if request should be retried</returns>
    private static bool ShouldRetryMcpRequest(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    #endregion
}
