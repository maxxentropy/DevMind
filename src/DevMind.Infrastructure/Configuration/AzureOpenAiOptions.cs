// src/DevMind.Infrastructure/Configuration/AzureOpenAiOptions.cs

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Configuration options specific to Azure OpenAI Service integration.
/// Handles Azure-specific settings, deployments, and enterprise features.
/// </summary>
public class AzureOpenAiOptions : BaseLlmProviderOptions
{
    /// <summary>
    /// Azure OpenAI API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI resource endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI deployment name (model deployment)
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API version
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>
    /// Azure subscription ID (for enterprise features)
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Azure resource group name
    /// </summary>
    public string? ResourceGroup { get; set; }

    /// <summary>
    /// Azure OpenAI resource name
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Azure AD tenant ID for authentication
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD client ID for service principal authentication
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Azure AD client secret for service principal authentication
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Whether to use Azure AD authentication instead of API key
    /// </summary>
    public bool UseAzureAdAuth { get; set; } = false;

    /// <summary>
    /// Whether to use managed identity for authentication
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Custom stop sequences for Azure OpenAI responses
    /// </summary>
    public List<string> StopSequences { get; set; } = new();

    /// <summary>
    /// Whether to enable streaming responses
    /// </summary>
    public bool EnableStreaming { get; set; } = false;

    /// <summary>
    /// Number of chat completion choices to generate
    /// </summary>
    public int NumberOfChoices { get; set; } = 1;

    /// <summary>
    /// Frequency penalty for reducing repetition (-2.0 to 2.0)
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Presence penalty for encouraging new topics (-2.0 to 2.0)
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;

    /// <summary>
    /// Azure-specific content filtering settings
    /// </summary>
    public AzureContentFilterSettings ContentFilter { get; set; } = new();

    /// <summary>
    /// Azure-specific monitoring and logging settings
    /// </summary>
    public AzureMonitoringSettings Monitoring { get; set; } = new();

    /// <summary>
    /// Azure-specific networking and security settings
    /// </summary>
    public AzureNetworkingSettings Networking { get; set; } = new();

    /// <summary>
    /// Azure-specific cost management settings
    /// </summary>
    public AzureCostSettings Cost { get; set; } = new();

    /// <summary>
    /// Available Azure OpenAI deployments and their model mappings
    /// </summary>
    public static readonly Dictionary<string, AzureDeploymentInfo> CommonDeployments = new()
    {
        ["gpt-35-turbo"] = new()
        {
            ModelName = "gpt-3.5-turbo",
            ModelVersion = "0125",
            MaxTokens = 4096,
            ContextWindow = 16385,
            SupportsVision = false,
            SupportsFunction = true,
            RecommendedForProduction = true
        },
        ["gpt-35-turbo-16k"] = new()
        {
            ModelName = "gpt-3.5-turbo-16k",
            ModelVersion = "0613",
            MaxTokens = 16384,
            ContextWindow = 16385,
            SupportsVision = false,
            SupportsFunction = true,
            RecommendedForProduction = true
        },
        ["gpt-4"] = new()
        {
            ModelName = "gpt-4",
            ModelVersion = "0613",
            MaxTokens = 8192,
            ContextWindow = 8192,
            SupportsVision = false,
            SupportsFunction = true,
            RecommendedForProduction = true
        },
        ["gpt-4-turbo"] = new()
        {
            ModelName = "gpt-4-turbo",
            ModelVersion = "2024-04-09",
            MaxTokens = 4096,
            ContextWindow = 128000,
            SupportsVision = true,
            SupportsFunction = true,
            RecommendedForProduction = true
        },
        ["gpt-4-vision"] = new()
        {
            ModelName = "gpt-4-vision-preview",
            ModelVersion = "vision-preview",
            MaxTokens = 4096,
            ContextWindow = 128000,
            SupportsVision = true,
            SupportsFunction = false,
            RecommendedForProduction = false
        }
    };

    /// <summary>
    /// Gets the deployment information for the currently configured deployment
    /// </summary>
    public AzureDeploymentInfo? GetCurrentDeploymentInfo()
    {
        return CommonDeployments.TryGetValue(DeploymentName, out var info) ? info : null;
    }

    /// <summary>
    /// Gets the recommended deployment for a specific task type
    /// </summary>
    /// <param name="taskType">The type of task</param>
    /// <returns>Recommended deployment name</returns>
    public static string? GetRecommendedDeploymentForTask(string taskType)
    {
        return taskType.ToLowerInvariant() switch
        {
            "reasoning" or "complex-analysis" => "gpt-4-turbo",
            "coding" or "development" => "gpt-4-turbo",
            "vision" or "image-analysis" => "gpt-4-vision",
            "fast" or "simple" => "gpt-35-turbo",
            "cost-effective" => "gpt-35-turbo",
            "long-context" => "gpt-35-turbo-16k",
            _ => "gpt-4-turbo"
        };
    }

    /// <summary>
    /// Validates Azure OpenAI-specific configuration settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public override List<string> Validate()
    {
        var errors = ValidateBase();

        // Validate authentication configuration
        if (UseAzureAdAuth || UseManagedIdentity)
        {
            if (UseAzureAdAuth && UseManagedIdentity)
            {
                errors.Add("Cannot use both Azure AD authentication and Managed Identity simultaneously");
            }

            if (UseAzureAdAuth && string.IsNullOrWhiteSpace(TenantId))
            {
                errors.Add("TenantId is required when using Azure AD authentication");
            }

            if (UseAzureAdAuth && string.IsNullOrWhiteSpace(ClientId))
            {
                errors.Add("ClientId is required when using Azure AD authentication");
            }

            if (UseAzureAdAuth && string.IsNullOrWhiteSpace(ClientSecret))
            {
                errors.Add("ClientSecret is required when using Azure AD authentication");
            }
        }
        else
        {
            // API key authentication
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                errors.Add("Azure OpenAI ApiKey is required when not using Azure AD or Managed Identity");
            }
        }

        // Validate endpoint
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            errors.Add("Azure OpenAI Endpoint is required");
        }
        else if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("Endpoint must be a valid HTTP/HTTPS URL");
        }
        else if (!Endpoint.Contains("openai.azure.com"))
        {
            errors.Add("Endpoint should be an Azure OpenAI endpoint (*.openai.azure.com)");
        }

        // Validate deployment name
        if (string.IsNullOrWhiteSpace(DeploymentName))
        {
            errors.Add("DeploymentName is required for Azure OpenAI");
        }

        // Validate API version
        if (string.IsNullOrWhiteSpace(ApiVersion))
        {
            errors.Add("ApiVersion is required for Azure OpenAI");
        }

        // Validate penalties
        if (FrequencyPenalty < -2.0 || FrequencyPenalty > 2.0)
        {
            errors.Add("FrequencyPenalty must be between -2.0 and 2.0");
        }

        if (PresencePenalty < -2.0 || PresencePenalty > 2.0)
        {
            errors.Add("PresencePenalty must be between -2.0 and 2.0");
        }

        // Validate number of choices
        if (NumberOfChoices < 1 || NumberOfChoices > 10)
        {
            errors.Add("NumberOfChoices must be between 1 and 10");
        }

        // Validate deployment against known deployments
        var deploymentInfo = GetCurrentDeploymentInfo();
        if (deploymentInfo != null && MaxTokens > deploymentInfo.MaxTokens)
        {
            errors.Add($"MaxTokens ({MaxTokens}) exceeds deployment limit ({deploymentInfo.MaxTokens}) for {DeploymentName}");
        }

        // Validate nested settings
        errors.AddRange(ContentFilter.Validate());
        errors.AddRange(Monitoring.Validate());
        errors.AddRange(Networking.Validate());
        errors.AddRange(Cost.Validate());

        return errors;
    }

    /// <summary>
    /// Gets a safe configuration summary for logging (excludes sensitive data)
    /// </summary>
    /// <returns>Configuration summary without sensitive data</returns>
    public string GetSafeConfigurationSummary()
    {
        var deploymentInfo = GetCurrentDeploymentInfo();
        var hasApiKey = !string.IsNullOrWhiteSpace(ApiKey);

        return $"Deployment: {DeploymentName} ({deploymentInfo?.ModelName ?? "Unknown"}), " +
               $"Endpoint: {Endpoint}, " +
               $"ApiVersion: {ApiVersion}, " +
               $"MaxTokens: {MaxTokens}, " +
               $"Temperature: {Temperature}, " +
               $"Auth: {GetAuthenticationMethod()}, " +
               $"ApiKey: {(hasApiKey ? "***configured***" : "NOT SET")}, " +
               $"Streaming: {EnableStreaming}";
    }

    /// <summary>
    /// Gets the authentication method being used
    /// </summary>
    /// <returns>Authentication method description</returns>
    public string GetAuthenticationMethod()
    {
        if (UseManagedIdentity) return "Managed Identity";
        if (UseAzureAdAuth) return "Azure AD";
        return "API Key";
    }

    /// <summary>
    /// Creates a copy of the options with sensitive data redacted
    /// </summary>
    /// <returns>Copy with sensitive data removed</returns>
    public AzureOpenAiOptions CreateRedactedCopy()
    {
        var copy = new AzureOpenAiOptions
        {
            Endpoint = Endpoint,
            DeploymentName = DeploymentName,
            ApiVersion = ApiVersion,
            SubscriptionId = SubscriptionId,
            ResourceGroup = ResourceGroup,
            ResourceName = ResourceName,
            TenantId = TenantId,
            ClientId = ClientId,
            UseAzureAdAuth = UseAzureAdAuth,
            UseManagedIdentity = UseManagedIdentity,
            Model = Model,
            MaxTokens = MaxTokens,
            Temperature = Temperature,
            TopP = TopP,
            FrequencyPenalty = FrequencyPenalty,
            PresencePenalty = PresencePenalty,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetries = MaxRetries,
            Enabled = Enabled,
            StopSequences = new List<string>(StopSequences),
            EnableStreaming = EnableStreaming,
            NumberOfChoices = NumberOfChoices,
            ContentFilter = ContentFilter,
            Monitoring = Monitoring,
            Networking = Networking,
            Cost = Cost,
            AdditionalSettings = new Dictionary<string, object>(AdditionalSettings),

            // Redacted sensitive fields
            ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? string.Empty : "***REDACTED***",
            ClientSecret = string.IsNullOrWhiteSpace(ClientSecret) ? null : "***REDACTED***"
        };
        return copy;
    }

    /// <summary>
    /// Checks if the current deployment supports a specific capability
    /// </summary>
    /// <param name="capability">The capability to check</param>
    /// <returns>True if supported</returns>
    public bool SupportsCapability(string capability)
    {
        var deploymentInfo = GetCurrentDeploymentInfo();
        if (deploymentInfo == null) return false;

        return capability.ToLowerInvariant() switch
        {
            "vision" => deploymentInfo.SupportsVision,
            "function-calling" => deploymentInfo.SupportsFunction,
            _ => false
        };
    }
}

/// <summary>
/// Information about an Azure OpenAI deployment
/// </summary>
public class AzureDeploymentInfo
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsFunction { get; set; }
    public bool RecommendedForProduction { get; set; }
    public string? Description { get; set; }
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Azure OpenAI content filtering settings
/// </summary>
public class AzureContentFilterSettings
{
    /// <summary>
    /// Whether to enable Azure's content filtering
    /// </summary>
    public bool EnableContentFiltering { get; set; } = true;

    /// <summary>
    /// Content filter severity level for hate content
    /// </summary>
    public ContentFilterSeverity HateFilterLevel { get; set; } = ContentFilterSeverity.Medium;

    /// <summary>
    /// Content filter severity level for sexual content
    /// </summary>
    public ContentFilterSeverity SexualFilterLevel { get; set; } = ContentFilterSeverity.Medium;

    /// <summary>
    /// Content filter severity level for violence content
    /// </summary>
    public ContentFilterSeverity ViolenceFilterLevel { get; set; } = ContentFilterSeverity.Medium;

    /// <summary>
    /// Content filter severity level for self-harm content
    /// </summary>
    public ContentFilterSeverity SelfHarmFilterLevel { get; set; } = ContentFilterSeverity.Medium;

    /// <summary>
    /// Whether to block content that triggers filters
    /// </summary>
    public bool BlockFilteredContent { get; set; } = true;

    /// <summary>
    /// Whether to log filtered content for monitoring
    /// </summary>
    public bool LogFilteredContent { get; set; } = true;

    /// <summary>
    /// Custom content filter policies
    /// </summary>
    public List<string> CustomFilterPolicies { get; set; } = new();

    /// <summary>
    /// Validates content filter settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        // Content filter settings are generally valid by default
        return errors;
    }
}

/// <summary>
/// Azure monitoring and diagnostics settings
/// </summary>
public class AzureMonitoringSettings
{
    /// <summary>
    /// Whether to enable Azure Application Insights integration
    /// </summary>
    public bool EnableApplicationInsights { get; set; } = true;

    /// <summary>
    /// Application Insights connection string
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }

    /// <summary>
    /// Whether to enable detailed request/response logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable performance metrics collection
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Custom telemetry properties to include
    /// </summary>
    public Dictionary<string, string> CustomTelemetryProperties { get; set; } = new();

    /// <summary>
    /// Log retention period in days
    /// </summary>
    public int LogRetentionDays { get; set; } = 30;

    /// <summary>
    /// Validates monitoring settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (LogRetentionDays < 1 || LogRetentionDays > 365)
        {
            errors.Add("LogRetentionDays must be between 1 and 365");
        }

        return errors;
    }
}

/// <summary>
/// Azure networking and security settings
/// </summary>
public class AzureNetworkingSettings
{
    /// <summary>
    /// Whether to use private endpoints
    /// </summary>
    public bool UsePrivateEndpoints { get; set; } = false;

    /// <summary>
    /// Virtual network name for private endpoints
    /// </summary>
    public string? VirtualNetworkName { get; set; }

    /// <summary>
    /// Subnet name for private endpoints
    /// </summary>
    public string? SubnetName { get; set; }

    /// <summary>
    /// Whether to restrict access to specific IP ranges
    /// </summary>
    public bool EnableIpRestrictions { get; set; } = false;

    /// <summary>
    /// Allowed IP address ranges (CIDR notation)
    /// </summary>
    public List<string> AllowedIpRanges { get; set; } = new();

    /// <summary>
    /// Whether to enable Azure Key Vault integration for secrets
    /// </summary>
    public bool EnableKeyVaultIntegration { get; set; } = false;

    /// <summary>
    /// Key Vault URL for secret storage
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Validates networking settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (UsePrivateEndpoints && string.IsNullOrWhiteSpace(VirtualNetworkName))
        {
            errors.Add("VirtualNetworkName is required when using private endpoints");
        }

        if (EnableKeyVaultIntegration && string.IsNullOrWhiteSpace(KeyVaultUrl))
        {
            errors.Add("KeyVaultUrl is required when Key Vault integration is enabled");
        }

        return errors;
    }
}

/// <summary>
/// Azure-specific cost management settings
/// </summary>
public class AzureCostSettings
{
    /// <summary>
    /// Whether to enable cost tracking via Azure Cost Management
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;

    /// <summary>
    /// Azure budget ID for cost monitoring
    /// </summary>
    public string? BudgetId { get; set; }

    /// <summary>
    /// Daily spending limit in USD
    /// </summary>
    public decimal DailySpendingLimit { get; set; } = 0m;

    /// <summary>
    /// Monthly spending limit in USD
    /// </summary>
    public decimal MonthlySpendingLimit { get; set; } = 1000m;

    /// <summary>
    /// Whether to enable Azure alerts for cost thresholds
    /// </summary>
    public bool EnableAzureAlerts { get; set; } = true;

    /// <summary>
    /// Cost alert threshold percentage (0.8 = 80%)
    /// </summary>
    public double AlertThreshold { get; set; } = 0.8;

    /// <summary>
    /// Whether to enable automatic scaling based on usage
    /// </summary>
    public bool EnableAutoScaling { get; set; } = false;

    /// <summary>
    /// Validates cost settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DailySpendingLimit < 0)
            errors.Add("DailySpendingLimit must be 0 or greater");

        if (MonthlySpendingLimit < 0)
            errors.Add("MonthlySpendingLimit must be 0 or greater");

        if (AlertThreshold < 0 || AlertThreshold > 1)
            errors.Add("AlertThreshold must be between 0 and 1");

        return errors;
    }
}

/// <summary>
/// Content filter severity levels
/// </summary>
public enum ContentFilterSeverity
{
    /// <summary>
    /// No filtering
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Low severity filtering
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity filtering
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity filtering
    /// </summary>
    High = 3
}
