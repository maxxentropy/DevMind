namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// Configuration options for MCP (Model Context Protocol) client
/// </summary>
public class McpClientOptions
{
    /// <summary>
    /// Base URL for the MCP server
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Whether to enable health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Validates the MCP client configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
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

        if (TimeoutSeconds <= 0)
        {
            errors.Add("TimeoutSeconds must be greater than 0");
        }

        if (RetryAttempts < 0)
        {
            errors.Add("RetryAttempts must be 0 or greater");
        }

        if (RetryDelaySeconds < 0)
        {
            errors.Add("RetryDelaySeconds must be 0 or greater");
        }

        if (HealthCheckIntervalSeconds <= 0)
        {
            errors.Add("HealthCheckIntervalSeconds must be greater than 0");
        }

        return errors;
    }
}
