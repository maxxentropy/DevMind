// src/DevMind.Infrastructure/Configuration/LlmServiceExtensions.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Application.Services;
using DevMind.Infrastructure.LlmProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring LLM services in the dependency injection container.
/// Provides a comprehensive setup for all LLM providers with proper configuration validation.
/// </summary>
public static class LlmServiceExtensions
{
    /// <summary>
    /// Adds all LLM services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from configuration
        services.ConfigureLlmOptions(configuration);

        // Register core application services
        services.AddLlmApplicationServices();

        // Register LLM provider implementations
        services.AddLlmProviders();

        // Register HTTP clients for external APIs
        services.AddLlmHttpClients(configuration);

        // Register the main LLM service with provider selection
        services.AddLlmServiceWithProviderSelection();

        // Add health checks for LLM providers
        services.AddLlmHealthChecks();

        // Add performance monitoring and metrics
        services.AddLlmTelemetry();

        // Add error handling
        services.AddLlmErrorHandling();

        // Add configuration validation
        services.AddLlmConfigurationValidation();

        return services;
    }

    /// <summary>
    /// Configures LLM options from application configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureLlmOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Main LLM provider configuration
        services.Configure<LlmProviderOptions>(configuration.GetSection("Llm"));

        // Provider-specific configurations
        services.Configure<OpenAiOptions>(configuration.GetSection("Llm:OpenAi"));
        services.Configure<AnthropicOptions>(configuration.GetSection("Llm:Anthropic"));
        services.Configure<OllamaOptions>(configuration.GetSection("Llm:Ollama"));
        services.Configure<AzureOpenAiOptions>(configuration.GetSection("Llm:AzureOpenAi"));

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<LlmProviderOptions>, LlmProviderOptionsValidator>();
        services.AddSingleton<IValidateOptions<OpenAiOptions>, OpenAiOptionsValidator>();
        services.AddSingleton<IValidateOptions<AnthropicOptions>, AnthropicOptionsValidator>();
        services.AddSingleton<IValidateOptions<OllamaOptions>, OllamaOptionsValidator>();
        services.AddSingleton<IValidateOptions<AzureOpenAiOptions>, AzureOpenAiOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Registers core LLM application services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmApplicationServices(this IServiceCollection services)
    {
        // Core application services
        services.AddScoped<IIntentAnalysisService, IntentAnalysisService>();
        services.AddScoped<IPlanningService, TaskPlanningService>();
        services.AddScoped<ISynthesisService, ResponseSynthesisService>();

        // Service factory for creating provider-specific services
        services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();

        return services;
    }

    /// <summary>
    /// Registers all LLM provider implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmProviders(this IServiceCollection services)
    {
        // Register individual provider services
        services.AddScoped<OpenAiService>();
        services.AddScoped<AnthropicService>();
        services.AddScoped<OllamaService>();
        services.AddScoped<AzureOpenAiService>();

        // Register provider factory
        //services.AddScoped<Func<string, ILlmService>>(serviceProvider => providerName =>
        //{
        //    return providerName.ToLowerInvariant() switch
        //    {
        //        "openai" => serviceProvider.GetRequiredService<OpenAiService>(),
        //        "anthropic" => serviceProvider.GetRequiredService<AnthropicService>(),
        //        "ollama" => serviceProvider.GetRequiredService<OllamaService>(),
        //        "azure-openai" => serviceProvider.GetRequiredService<AzureOpenAiService>(),
        //        _ => throw new ArgumentException($"Unknown LLM provider: {providerName}")
        //    };
        //});

        return services;
    }

    /// <summary>
    /// Configures HTTP clients for LLM providers
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // OpenAI HTTP client
        //services.AddHttpClient<OpenAiService>("OpenAI", (serviceProvider, client) =>
        //{
        //    var options = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
        //    ConfigureOpenAiHttpClient(client, options);
        //})
        //.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        //.AddPolicyHandler(GetRetryPolicy())
        //.AddPolicyHandler(GetTimeoutPolicy());

        //// Anthropic HTTP client
        //services.AddHttpClient<AnthropicService>("Anthropic", (serviceProvider, client) =>
        //{
        //    var options = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
        //    ConfigureAnthropicHttpClient(client, options);
        //})
        //.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        //.AddPolicyHandler(GetRetryPolicy())
        //.AddPolicyHandler(GetTimeoutPolicy());

        //// Ollama HTTP client
        //services.AddHttpClient<OllamaService>("Ollama", (serviceProvider, client) =>
        //{
        //    var options = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>().Value;
        //    ConfigureOllamaHttpClient(client, options);
        //})
        //.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        //.AddPolicyHandler(GetRetryPolicy())
        //.AddPolicyHandler(GetTimeoutPolicy());

        //// Azure OpenAI HTTP client
        //services.AddHttpClient<AzureOpenAiService>("AzureOpenAI", (serviceProvider, client) =>
        //{
        //    var options = serviceProvider.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;
        //    ConfigureAzureOpenAiHttpClient(client, options);
        //})
        //.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        //.AddPolicyHandler(GetRetryPolicy())
        //.AddPolicyHandler(GetTimeoutPolicy());

        return services;
    }

    /// <summary>
    /// Adds LLM configuration validation as a hosted service
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmConfigurationValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<LlmConfigurationValidator>();
        return services;
    }

    /// <summary>
    /// Adds LLM error handling services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmErrorHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<LlmErrorHandler>();
        return services;
    }

    /// <summary>
    /// Registers the main LLM service with provider selection logic
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmServiceWithProviderSelection(this IServiceCollection services)
    {
        services.AddScoped<ILlmService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<ILlmService>>();
            var providerFactory = serviceProvider.GetRequiredService<Func<string, ILlmService>>();

            try
            {
                var provider = providerFactory(options.Provider);
                logger.LogInformation("Using LLM provider: {Provider}", options.Provider);
                return provider;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create LLM provider {Provider}", options.Provider);

                // Fallback mechanism
                //if (options.Global.EnableFallback && options.FallbackProviders.Any())
                //{
                //    foreach (var fallbackProvider in options.FallbackProviders)
                //    {
                //        try
                //        {
                //            logger.LogWarning("Attempting fallback to provider: {FallbackProvider}", fallbackProvider);
                //            return providerFactory(fallbackProvider);
                //        }
                //        catch
                //        {
                //            logger.LogWarning("Fallback provider {FallbackProvider} also failed", fallbackProvider);
                //        }
                //    }
                //}

                throw new InvalidOperationException($"Unable to create LLM provider {options.Provider} and all fallbacks failed", ex);
            }
        });

        return services;
    }

    /// <summary>
    /// Adds health checks for LLM providers
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmHealthChecks(this IServiceCollection services)
    {
        //services.AddHealthChecks()
        //    .AddCheck<LlmProviderHealthCheck>("llm-provider", timeout: TimeSpan.FromSeconds(30))
        //    .AddCheck<OpenAiHealthCheck>("openai", timeout: TimeSpan.FromSeconds(30))
        //    .AddCheck<AnthropicHealthCheck>("anthropic", timeout: TimeSpan.FromSeconds(30))
        //    .AddCheck<OllamaHealthCheck>("ollama", timeout: TimeSpan.FromSeconds(30));

        return services;
    }

    /// <summary>
    /// Adds telemetry and monitoring for LLM services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLlmTelemetry(this IServiceCollection services)
    {
        // Add metrics collection
        services.AddSingleton<ILlmMetricsCollector, LlmMetricsCollector>();

        // Add usage tracking
        services.AddScoped<ILlmUsageTracker, LlmUsageTracker>();

        // Add performance monitoring
        services.AddScoped<ILlmPerformanceMonitor, LlmPerformanceMonitor>();

        return services;
    }

    // ==================== HTTP CLIENT CONFIGURATION ====================

    private static void ConfigureOpenAiHttpClient(HttpClient client, OpenAiOptions options)
    {
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        client.DefaultRequestHeaders.Add("User-Agent", "DevMind/1.0");

        if (!string.IsNullOrEmpty(options.OrganizationId))
        {
            client.DefaultRequestHeaders.Add("OpenAI-Organization", options.OrganizationId);
        }

        if (!string.IsNullOrEmpty(options.ProjectId))
        {
            client.DefaultRequestHeaders.Add("OpenAI-Project", options.ProjectId);
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds ?? 30);
    }

    private static void ConfigureAnthropicHttpClient(HttpClient client, AnthropicOptions options)
    {
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", options.AnthropicVersion);
        client.DefaultRequestHeaders.Add("User-Agent", "DevMind/1.0");

        if (options.UseBetaFeatures)
        {
            client.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds ?? 30);
    }

    private static void ConfigureOllamaHttpClient(HttpClient client, OllamaOptions options)
    {
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Add("User-Agent", "DevMind/1.0");
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds ?? 60);
    }

    private static void ConfigureAzureOpenAiHttpClient(HttpClient client, AzureOpenAiOptions options)
    {
        client.BaseAddress = new Uri(options.Endpoint);
        client.DefaultRequestHeaders.Add("User-Agent", "DevMind/1.0");

        if (!options.UseAzureAdAuth && !options.UseManagedIdentity)
        {
            client.DefaultRequestHeaders.Add("api-key", options.ApiKey);
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds ?? 30);
    }

    // ==================== POLLY POLICIES ====================

    //    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    //    {
    //        return Policy
    //            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ShouldRetry(r.StatusCode))
    //            .WaitAndRetryAsync(
    //                retryCount: 3,
    //                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
    //                onRetry: (outcome, timespan, retryCount, context) =>
    //                {
    //                    var logger = context.GetLogger();
    //                    logger?.LogWarning("Retry {RetryCount} for LLM request after {Delay}ms",
    //                        retryCount, timespan.TotalMilliseconds);
    //                });
    //    }

    //    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    //    {
    //        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(120));
    //    }

    //    private static bool ShouldRetry(HttpStatusCode statusCode)
    //    {
    //        return statusCode switch
    //        {
    //            HttpStatusCode.RequestTimeout => true,
    //            HttpStatusCode.TooManyRequests => true,
    //            HttpStatusCode.InternalServerError => true,
    //            HttpStatusCode.BadGateway => true,
    //            HttpStatusCode.ServiceUnavailable => true,
    //            HttpStatusCode.GatewayTimeout => true,
    //            _ => false
    //        };
    //    }
    //}

    // ==================== PROVIDER FACTORY ====================

    /// <summary>
    /// Factory interface for creating LLM provider instances
    /// </summary>
    public interface ILlmProviderFactory
    {
        ILlmService CreateProvider(string providerName);
        IEnumerable<string> GetAvailableProviders();
        bool IsProviderAvailable(string providerName);
    }

    /// <summary>
    /// Factory implementation for creating LLM provider instances
    /// </summary>
    public class LlmProviderFactory : ILlmProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LlmProviderFactory> _logger;
        private readonly IOptions<LlmProviderOptions> _options;

        public LlmProviderFactory(
            IServiceProvider serviceProvider,
            ILogger<LlmProviderFactory> logger,
            IOptions<LlmProviderOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options;
        }

        public ILlmService CreateProvider(string providerName)
        {
            try
            {
                ILlmService? provider = providerName.ToLowerInvariant() switch
                {
                    "openai" => _serviceProvider.GetRequiredService<OpenAiService>(),
                    "anthropic" => _serviceProvider.GetRequiredService<AnthropicService>(),
                    "ollama" => _serviceProvider.GetRequiredService<OllamaService>(),
                    "azure-openai" => _serviceProvider.GetRequiredService<AzureOpenAiService>() as ILlmService, // Explicit cast
                    _ => throw new ArgumentException($"Unknown LLM provider: {providerName}")
                };

                _logger.LogDebug("Created LLM provider: {ProviderName}", providerName);
                return provider!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create LLM provider: {ProviderName}", providerName);
                throw;
            }
        }

        public IEnumerable<string> GetAvailableProviders()
        {
            return new[] { "openai", "anthropic", "ollama", "azure-openai" };
        }

        public bool IsProviderAvailable(string providerName)
        {
            try
            {
                CreateProvider(providerName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // ==================== CONFIGURATION VALIDATORS ====================

    /// <summary>
    /// Validates LLM provider options on startup
    /// </summary>
    public class LlmProviderOptionsValidator : IValidateOptions<LlmProviderOptions>
    {
        public ValidateOptionsResult Validate(string? name, LlmProviderOptions options)
        {
            var errors = options.Validate();

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates OpenAI options on startup
    /// </summary>
    public class OpenAiOptionsValidator : IValidateOptions<OpenAiOptions>
    {
        public ValidateOptionsResult Validate(string? name, OpenAiOptions options)
        {
            var errors = options.Validate();

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates Anthropic options on startup
    /// </summary>
    public class AnthropicOptionsValidator : IValidateOptions<AnthropicOptions>
    {
        public ValidateOptionsResult Validate(string? name, AnthropicOptions options)
        {
            var errors = options.Validate();

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates Ollama options on startup
    /// </summary>
    public class OllamaOptionsValidator : IValidateOptions<OllamaOptions>
    {
        public ValidateOptionsResult Validate(string? name, OllamaOptions options)
        {
            var errors = options.Validate();

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates Azure OpenAI options on startup
    /// </summary>
    public class AzureOpenAiOptionsValidator : IValidateOptions<AzureOpenAiOptions>
    {
        public ValidateOptionsResult Validate(string? name, AzureOpenAiOptions options)
        {
            var errors = options.Validate();

            if (errors.Any())
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }

    // ==================== PLACEHOLDER INTERFACES ====================
    // Note: These interfaces need to be implemented in separate files

    public interface ILlmMetricsCollector
    {
        void RecordRequest(string provider, string model, int promptTokens, int completionTokens, TimeSpan duration);
        void RecordError(string provider, string error);
    }

    public interface ILlmUsageTracker
    {
        Task TrackUsageAsync(string provider, string model, int promptTokens, int completionTokens, decimal cost);
        Task<decimal> GetMonthlyUsageAsync(string provider);
    }

    public interface ILlmPerformanceMonitor
    {
        void StartRequest(string requestId);
        void EndRequest(string requestId, bool success);
        TimeSpan GetAverageResponseTime(string provider);
    }

    // Health check placeholders - these will be implemented later
    //public class LlmProviderHealthCheck : IHealthCheck
    //{
    //    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    //    {
    //        // TODO: Implement comprehensive health check
    //        return Task.FromResult(HealthCheckResult.Healthy("LLM provider health check placeholder"));
    //    }
    //}

    //public class OpenAiHealthCheck : IHealthCheck
    //{
    //    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    //    {
    //        // TODO: Implement OpenAI-specific health check
    //        return Task.FromResult(HealthCheckResult.Healthy("OpenAI health check placeholder"));
    //    }
    //}

    //public class AnthropicHealthCheck : IHealthCheck
    //{
    //    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    //    {
    //        // TODO: Implement Anthropic-specific health check
    //        return Task.FromResult(HealthCheckResult.Healthy("Anthropic health check placeholder"));
    //    }
    //}

    //public class OllamaHealthCheck : IHealthCheck
    //{
    //    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    //    {
    //        // TODO: Implement Ollama-specific health check
    //        return Task.FromResult(HealthCheckResult.Healthy("Ollama health check placeholder"));
    //    }
    //}

    // Placeholder implementations for metrics and monitoring
    public class LlmMetricsCollector : ILlmMetricsCollector
    {
        public void RecordRequest(string provider, string model, int promptTokens, int completionTokens, TimeSpan duration)
        {
            // TODO: Implement metrics collection
        }

        public void RecordError(string provider, string error)
        {
            // TODO: Implement error metrics
        }
    }

    public class LlmUsageTracker : ILlmUsageTracker
    {
        public Task TrackUsageAsync(string provider, string model, int promptTokens, int completionTokens, decimal cost)
        {
            // TODO: Implement usage tracking
            return Task.CompletedTask;
        }

        public Task<decimal> GetMonthlyUsageAsync(string provider)
        {
            // TODO: Implement usage retrieval
            return Task.FromResult(0m);
        }
    }

    public class LlmPerformanceMonitor : ILlmPerformanceMonitor
    {
        public void StartRequest(string requestId)
        {
            // TODO: Implement performance monitoring
        }

        public void EndRequest(string requestId, bool success)
        {
            // TODO: Implement performance monitoring
        }

        public TimeSpan GetAverageResponseTime(string provider)
        {
            // TODO: Implement performance monitoring
            return TimeSpan.Zero;
        }
    }
}
