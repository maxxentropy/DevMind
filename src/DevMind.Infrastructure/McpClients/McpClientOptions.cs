namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// Configuration options for MCP (Model Context Protocol) client
/// Handles connection settings, timeouts, retry policies, and protocol-specific options
/// </summary>
public class McpClientOptions
{
    #region Core Connection Settings

    /// <summary>
    /// Base URL for the MCP server
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    #endregion

    #region Protocol Settings

    /// <summary>
    /// MCP protocol version to negotiate with server
    /// </summary>
    public string ProtocolVersion { get; set; } = "2024-11-05";

    /// <summary>
    /// Client identification information
    /// </summary>
    public McpClientIdentity ClientIdentity { get; set; } = new();

    /// <summary>
    /// Client capabilities to advertise to server
    /// </summary>
    public McpClientCapabilities ClientCapabilities { get; set; } = new();

    /// <summary>
    /// Whether to automatically initialize connection on first use
    /// </summary>
    public bool AutoInitialize { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    #endregion

    #region Health Check Settings

    /// <summary>
    /// Whether to enable periodic health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Number of consecutive health check failures before marking server as unhealthy
    /// </summary>
    public int MaxHealthCheckFailures { get; set; } = 3;

    /// <summary>
    /// Whether to automatically reconnect after health check failures
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    #endregion

    #region Logging and Monitoring

    /// <summary>
    /// Whether to enable detailed request/response logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Whether to log request/response payloads (useful for debugging)
    /// </summary>
    public bool LogPayloads { get; set; } = false;

    /// <summary>
    /// Whether to collect performance metrics
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Whether to enable distributed tracing
    /// </summary>
    public bool EnableTracing { get; set; } = false;

    #endregion

    #region Security Settings

    /// <summary>
    /// Authentication configuration
    /// </summary>
    public McpAuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// Whether to validate SSL certificates (disable only for development)
    /// </summary>
    public bool ValidateSslCertificates { get; set; } = true;

    /// <summary>
    /// Custom HTTP headers to include with all requests
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    #endregion

    #region Performance Settings

    /// <summary>
    /// HTTP connection pool settings
    /// </summary>
    public McpConnectionPoolOptions ConnectionPool { get; set; } = new();

    /// <summary>
    /// Request compression settings
    /// </summary>
    public McpCompressionOptions Compression { get; set; } = new();

    /// <summary>
    /// Response caching settings
    /// </summary>
    public McpCachingOptions Caching { get; set; } = new();

    #endregion

    #region Validation

    /// <summary>
    /// Validates the MCP client configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate connection settings
        errors.AddRange(ValidateConnectionSettings());

        // Validate protocol settings
        errors.AddRange(ValidateProtocolSettings());

        // Validate health check settings
        errors.AddRange(ValidateHealthCheckSettings());

        // Validate nested configurations
        errors.AddRange(ClientIdentity.Validate());
        errors.AddRange(ClientCapabilities.Validate());
        errors.AddRange(Authentication.Validate());
        errors.AddRange(ConnectionPool.Validate());
        errors.AddRange(Compression.Validate());
        errors.AddRange(Caching.Validate());

        return errors;
    }

    private List<string> ValidateConnectionSettings()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            errors.Add("BaseUrl is required");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("BaseUrl must be a valid HTTP/HTTPS URL");
        }

        if (TimeoutSeconds <= 0 || TimeoutSeconds > 300)
        {
            errors.Add("TimeoutSeconds must be between 1 and 300");
        }

        if (RetryAttempts < 0 || RetryAttempts > 10)
        {
            errors.Add("RetryAttempts must be between 0 and 10");
        }

        if (RetryDelaySeconds < 0 || RetryDelaySeconds > 60)
        {
            errors.Add("RetryDelaySeconds must be between 0 and 60");
        }

        if (MaxConcurrentRequests <= 0 || MaxConcurrentRequests > 100)
        {
            errors.Add("MaxConcurrentRequests must be between 1 and 100");
        }

        return errors;
    }

    private List<string> ValidateProtocolSettings()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ProtocolVersion))
        {
            errors.Add("ProtocolVersion is required");
        }

        return errors;
    }

    private List<string> ValidateHealthCheckSettings()
    {
        var errors = new List<string>();

        if (EnableHealthChecks)
        {
            if (HealthCheckIntervalSeconds <= 0 || HealthCheckIntervalSeconds > 3600)
            {
                errors.Add("HealthCheckIntervalSeconds must be between 1 and 3600 when health checks are enabled");
            }

            if (MaxHealthCheckFailures <= 0 || MaxHealthCheckFailures > 10)
            {
                errors.Add("MaxHealthCheckFailures must be between 1 and 10");
            }
        }

        return errors;
    }

    #endregion

    #region Configuration Summary

    /// <summary>
    /// Gets a safe configuration summary for logging
    /// </summary>
    /// <returns>Configuration summary without sensitive data</returns>
    public string GetConfigurationSummary()
    {
        return $"BaseUrl: {BaseUrl}, " +
               $"Protocol: {ProtocolVersion}, " +
               $"Timeout: {TimeoutSeconds}s, " +
               $"Retries: {RetryAttempts}, " +
               $"HealthChecks: {EnableHealthChecks}, " +
               $"Auth: {Authentication.GetAuthType()}, " +
               $"MaxConcurrent: {MaxConcurrentRequests}";
    }

    #endregion
}

#region Supporting Configuration Classes

/// <summary>
/// MCP client identity information
/// </summary>
public class McpClientIdentity
{
    /// <summary>
    /// Client name identifier
    /// </summary>
    public string Name { get; set; } = "DevMind";

    /// <summary>
    /// Client version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Optional client description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Client vendor information
    /// </summary>
    public string? Vendor { get; set; }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Client name is required");

        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("Client version is required");

        return errors;
    }
}

/// <summary>
/// MCP client capabilities configuration
/// </summary>
public class McpClientCapabilities
{
    /// <summary>
    /// Whether client supports roots capability
    /// </summary>
    public bool SupportsRoots { get; set; } = true;

    /// <summary>
    /// Whether client supports roots list change notifications
    /// </summary>
    public bool SupportsRootsListChanged { get; set; } = true;

    /// <summary>
    /// Whether client supports sampling
    /// </summary>
    public bool SupportsSampling { get; set; } = false;

    /// <summary>
    /// Custom capabilities to advertise
    /// </summary>
    public Dictionary<string, object> CustomCapabilities { get; set; } = new();

    public List<string> Validate()
    {
        // No specific validation rules for capabilities
        return new List<string>();
    }
}

/// <summary>
/// Authentication configuration for MCP client
/// </summary>
public class McpAuthenticationOptions
{
    /// <summary>
    /// Authentication type
    /// </summary>
    public McpAuthType AuthType { get; set; } = McpAuthType.None;

    /// <summary>
    /// API key for key-based authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Bearer token for token-based authentication
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Username for basic authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for basic authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Custom authentication headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    public string GetAuthType()
    {
        return AuthType.ToString();
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        switch (AuthType)
        {
            case McpAuthType.ApiKey when string.IsNullOrWhiteSpace(ApiKey):
                errors.Add("ApiKey is required when using API key authentication");
                break;

            case McpAuthType.Bearer when string.IsNullOrWhiteSpace(BearerToken):
                errors.Add("BearerToken is required when using bearer token authentication");
                break;

            case McpAuthType.Basic when string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password):
                errors.Add("Username and Password are required when using basic authentication");
                break;
        }

        return errors;
    }
}

/// <summary>
/// HTTP connection pool configuration
/// </summary>
public class McpConnectionPoolOptions
{
    /// <summary>
    /// Maximum number of connections in the pool
    /// </summary>
    public int MaxConnections { get; set; } = 20;

    /// <summary>
    /// Connection idle timeout in seconds
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Connection lifetime in seconds
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 600;

    /// <summary>
    /// Whether to pool connections
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxConnections <= 0 || MaxConnections > 100)
            errors.Add("MaxConnections must be between 1 and 100");

        if (IdleTimeoutSeconds <= 0)
            errors.Add("IdleTimeoutSeconds must be greater than 0");

        if (ConnectionLifetimeSeconds <= 0)
            errors.Add("ConnectionLifetimeSeconds must be greater than 0");

        return errors;
    }
}

/// <summary>
/// Request/response compression configuration
/// </summary>
public class McpCompressionOptions
{
    /// <summary>
    /// Whether to enable request compression
    /// </summary>
    public bool EnableRequestCompression { get; set; } = false;

    /// <summary>
    /// Whether to enable response compression
    /// </summary>
    public bool EnableResponseCompression { get; set; } = true;

    /// <summary>
    /// Compression algorithm to use
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.Gzip;

    /// <summary>
    /// Minimum response size in bytes to compress
    /// </summary>
    public int MinCompressionSize { get; set; } = 1024;

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MinCompressionSize < 0)
            errors.Add("MinCompressionSize must be 0 or greater");

        return errors;
    }
}

/// <summary>
/// Response caching configuration
/// </summary>
public class McpCachingOptions
{
    /// <summary>
    /// Whether to enable response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Default cache duration in minutes
    /// </summary>
    public int DefaultCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    public int MaxCacheSizeMb { get; set; } = 50;

    /// <summary>
    /// Cache eviction policy
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LeastRecentlyUsed;

    /// <summary>
    /// Methods to cache (empty list means cache all)
    /// </summary>
    public List<string> CacheableMethods { get; set; } = new() { "tools/list", "resources/list", "prompts/list" };

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DefaultCacheDurationMinutes <= 0)
            errors.Add("DefaultCacheDurationMinutes must be greater than 0");

        if (MaxCacheSizeMb <= 0)
            errors.Add("MaxCacheSizeMb must be greater than 0");

        return errors;
    }
}

#endregion

#region Enumerations

/// <summary>
/// Supported authentication types for MCP client
/// </summary>
public enum McpAuthType
{
    /// <summary>
    /// No authentication
    /// </summary>
    None,

    /// <summary>
    /// API key authentication via header
    /// </summary>
    ApiKey,

    /// <summary>
    /// Bearer token authentication
    /// </summary>
    Bearer,

    /// <summary>
    /// Basic authentication with username/password
    /// </summary>
    Basic,

    /// <summary>
    /// Custom authentication via headers
    /// </summary>
    Custom
}

/// <summary>
/// Supported compression algorithms
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression
    /// </summary>
    None,

    /// <summary>
    /// Gzip compression
    /// </summary>
    Gzip,

    /// <summary>
    /// Deflate compression
    /// </summary>
    Deflate,

    /// <summary>
    /// Brotli compression
    /// </summary>
    Brotli
}

/// <summary>
/// Cache eviction policies
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// Least Recently Used (LRU)
    /// </summary>
    LeastRecentlyUsed,

    /// <summary>
    /// First In, First Out (FIFO)
    /// </summary>
    FirstInFirstOut,

    /// <summary>
    /// Least Frequently Used (LFU)
    /// </summary>
    LeastFrequentlyUsed,

    /// <summary>
    /// Time-based expiration only
    /// </summary>
    TimeBasedOnly
}

#endregion
