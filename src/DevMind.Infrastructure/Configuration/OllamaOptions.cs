// src/DevMind.Infrastructure/Configuration/OllamaOptions.cs

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Configuration options specific to Ollama local AI model integration.
/// Handles Ollama-specific settings, local models, and self-hosted configurations.
/// </summary>
public class OllamaOptions : BaseLlmProviderOptions
{
    /// <summary>
    /// Base URL for Ollama API server
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Ollama model to use. Defaults to Llama 2
    /// </summary>
    public new string Model { get; set; } = "llama2";

    /// <summary>
    /// Top-k sampling parameter for controlling randomness
    /// </summary>
    public int TopK { get; set; } = 40;

    /// <summary>
    /// Custom stop sequences for Ollama responses
    /// </summary>
    public List<string> StopSequences { get; set; } = new();

    /// <summary>
    /// Whether to enable streaming responses
    /// </summary>
    public bool EnableStreaming { get; set; } = false;

    /// <summary>
    /// Context window size for the model (number of tokens to remember)
    /// </summary>
    public int ContextSize { get; set; } = 2048;

    /// <summary>
    /// Number of GPU layers to use (-1 for auto-detection)
    /// </summary>
    public int GpuLayers { get; set; } = -1;

    /// <summary>
    /// Main GPU to use for computation
    /// </summary>
    public int MainGpu { get; set; } = 0;

    /// <summary>
    /// Whether to use memory mapping for model loading
    /// </summary>
    public bool UseMemoryMapping { get; set; } = true;

    /// <summary>
    /// Whether to lock memory to prevent swapping
    /// </summary>
    public bool LockMemory { get; set; } = false;

    /// <summary>
    /// Number of threads to use for computation
    /// </summary>
    public int NumThreads { get; set; } = 0; // 0 = auto-detect

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 512;

    /// <summary>
    /// Repeat penalty to reduce repetition
    /// </summary>
    public double RepeatPenalty { get; set; } = 1.1;

    /// <summary>
    /// Number of tokens to consider for repeat penalty
    /// </summary>
    public int RepeatLastN { get; set; } = 64;

    /// <summary>
    /// MIROSTAT sampling mode (0=disabled, 1=mirostat, 2=mirostat 2.0)
    /// </summary>
    public int MiroStat { get; set; } = 0;

    /// <summary>
    /// MIROSTAT target entropy (tau parameter)
    /// </summary>
    public double MiroStatTau { get; set; } = 5.0;

    /// <summary>
    /// MIROSTAT learning rate (eta parameter)
    /// </summary>
    public double MiroStatEta { get; set; } = 0.1;

    /// <summary>
    /// Tail free sampling parameter
    /// </summary>
    public double TailFreeSamplingZ { get; set; } = 1.0;

    /// <summary>
    /// Typical P sampling parameter
    /// </summary>
    public double TypicalP { get; set; } = 1.0;

    /// <summary>
    /// Random seed for reproducible generation (-1 for random)
    /// </summary>
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Custom model parameters for advanced users
    /// </summary>
    public Dictionary<string, object> CustomModelParameters { get; set; } = new();

    /// <summary>
    /// Ollama server management settings
    /// </summary>
    public OllamaServerSettings Server { get; set; } = new();

    /// <summary>
    /// Performance optimization settings
    /// </summary>
    public OllamaPerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Model management settings
    /// </summary>
    public OllamaModelSettings ModelManagement { get; set; } = new();

    /// <summary>
    /// Privacy and security settings
    /// </summary>
    public OllamaPrivacySettings Privacy { get; set; } = new();

    /// <summary>
    /// Available Ollama models with their specifications
    /// </summary>
    public static readonly Dictionary<string, OllamaModelInfo> AvailableModels = new()
    {
        ["llama2"] = new()
        {
            Name = "Llama 2",
            Family = "llama",
            Size = "7B",
            ParameterCount = 7_000_000_000,
            MemoryRequirement = "8GB",
            ContextLength = 4096,
            Capabilities = new[] { "general", "coding", "reasoning" },
            License = "Custom (Meta)",
            IsRecommended = true,
            DownloadSize = "3.8GB"
        },
        ["llama2:13b"] = new()
        {
            Name = "Llama 2 13B",
            Family = "llama",
            Size = "13B",
            ParameterCount = 13_000_000_000,
            MemoryRequirement = "16GB",
            ContextLength = 4096,
            Capabilities = new[] { "general", "coding", "reasoning", "analysis" },
            License = "Custom (Meta)",
            IsRecommended = false,
            DownloadSize = "7.3GB"
        },
        ["llama2:70b"] = new()
        {
            Name = "Llama 2 70B",
            Family = "llama",
            Size = "70B",
            ParameterCount = 70_000_000_000,
            MemoryRequirement = "80GB",
            ContextLength = 4096,
            Capabilities = new[] { "general", "coding", "reasoning", "analysis", "expert-level" },
            License = "Custom (Meta)",
            IsRecommended = false,
            DownloadSize = "39GB"
        },
        ["mistral"] = new()
        {
            Name = "Mistral",
            Family = "mistral",
            Size = "7B",
            ParameterCount = 7_000_000_000,
            MemoryRequirement = "8GB",
            ContextLength = 8192,
            Capabilities = new[] { "general", "coding", "fast-inference" },
            License = "Apache 2.0",
            IsRecommended = true,
            DownloadSize = "4.1GB"
        },
        ["codellama"] = new()
        {
            Name = "Code Llama",
            Family = "llama",
            Size = "7B",
            ParameterCount = 7_000_000_000,
            MemoryRequirement = "8GB",
            ContextLength = 16384,
            Capabilities = new[] { "coding", "programming", "development" },
            License = "Custom (Meta)",
            IsRecommended = true,
            DownloadSize = "3.8GB"
        },
        ["neural-chat"] = new()
        {
            Name = "Neural Chat",
            Family = "neural-chat",
            Size = "7B",
            ParameterCount = 7_000_000_000,
            MemoryRequirement = "8GB",
            ContextLength = 4096,
            Capabilities = new[] { "chat", "conversation", "assistant" },
            License = "Apache 2.0",
            IsRecommended = false,
            DownloadSize = "4.1GB"
        },
        ["orca-mini"] = new()
        {
            Name = "Orca Mini",
            Family = "orca",
            Size = "3B",
            ParameterCount = 3_000_000_000,
            MemoryRequirement = "4GB",
            ContextLength = 2048,
            Capabilities = new[] { "general", "lightweight", "fast" },
            License = "MIT",
            IsRecommended = false,
            DownloadSize = "1.9GB"
        }
    };

    /// <summary>
    /// Gets the model information for the currently configured model
    /// </summary>
    public OllamaModelInfo? GetCurrentModelInfo()
    {
        return AvailableModels.TryGetValue(Model, out var info) ? info : null;
    }

    /// <summary>
    /// Gets the recommended model for a specific task type
    /// </summary>
    /// <param name="taskType">The type of task</param>
    /// <returns>Recommended model name</returns>
    public static string? GetRecommendedModelForTask(string taskType)
    {
        return taskType.ToLowerInvariant() switch
        {
            "coding" or "programming" or "development" => "codellama",
            "fast" or "lightweight" or "quick" => "orca-mini",
            "general" or "analysis" or "reasoning" => "llama2",
            "conversation" or "chat" or "assistant" => "neural-chat",
            "performance" or "speed" => "mistral",
            _ => "llama2" // Default to Llama 2
        };
    }

    /// <summary>
    /// Validates Ollama-specific configuration settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public override List<string> Validate()
    {
        var errors = ValidateBase();

        // Validate base URL
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            errors.Add("BaseUrl is required");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("BaseUrl must be a valid HTTP/HTTPS URL");
        }

        // Validate model name
        if (string.IsNullOrWhiteSpace(Model))
        {
            errors.Add("Model is required");
        }

        // Validate TopK
        if (TopK <= 0 || TopK > 100)
        {
            errors.Add("TopK must be between 1 and 100");
        }

        // Validate context size
        if (ContextSize <= 0 || ContextSize > 32768)
        {
            errors.Add("ContextSize must be between 1 and 32768");
        }

        // Validate GPU layers
        if (GpuLayers < -1 || GpuLayers > 999)
        {
            errors.Add("GpuLayers must be -1 (auto) or between 0 and 999");
        }

        // Validate main GPU
        if (MainGpu < 0 || MainGpu > 7)
        {
            errors.Add("MainGpu must be between 0 and 7");
        }

        // Validate threads
        if (NumThreads < 0 || NumThreads > 64)
        {
            errors.Add("NumThreads must be 0 (auto) or between 1 and 64");
        }

        // Validate batch size
        if (BatchSize <= 0 || BatchSize > 2048)
        {
            errors.Add("BatchSize must be between 1 and 2048");
        }

        // Validate repeat penalty
        if (RepeatPenalty < 0.0 || RepeatPenalty > 2.0)
        {
            errors.Add("RepeatPenalty must be between 0.0 and 2.0");
        }

        // Validate repeat last N
        if (RepeatLastN < 0 || RepeatLastN > 1024)
        {
            errors.Add("RepeatLastN must be between 0 and 1024");
        }

        // Validate MIROSTAT
        if (MiroStat < 0 || MiroStat > 2)
        {
            errors.Add("MiroStat must be 0 (disabled), 1, or 2");
        }

        if (MiroStat > 0)
        {
            if (MiroStatTau <= 0 || MiroStatTau > 10)
            {
                errors.Add("MiroStatTau must be between 0 and 10 when MiroStat is enabled");
            }

            if (MiroStatEta <= 0 || MiroStatEta > 1)
            {
                errors.Add("MiroStatEta must be between 0 and 1 when MiroStat is enabled");
            }
        }

        // Validate sampling parameters
        if (TailFreeSamplingZ < 0 || TailFreeSamplingZ > 10)
        {
            errors.Add("TailFreeSamplingZ must be between 0 and 10");
        }

        if (TypicalP < 0 || TypicalP > 1)
        {
            errors.Add("TypicalP must be between 0 and 1");
        }

        // Validate nested settings
        errors.AddRange(Server.Validate());
        errors.AddRange(Performance.Validate());
        errors.AddRange(ModelManagement.Validate());
        errors.AddRange(Privacy.Validate());

        return errors;
    }

    /// <summary>
    /// Gets a safe configuration summary for logging
    /// </summary>
    /// <returns>Configuration summary</returns>
    public string GetSafeConfigurationSummary()
    {
        var modelInfo = GetCurrentModelInfo();

        return $"Model: {Model} ({modelInfo?.Name ?? "Unknown"}), " +
               $"BaseUrl: {BaseUrl}, " +
               $"ContextSize: {ContextSize}, " +
               $"MaxTokens: {MaxTokens}, " +
               $"Temperature: {Temperature}, " +
               $"TopK: {TopK}, " +
               $"GpuLayers: {GpuLayers}, " +
               $"Streaming: {EnableStreaming}, " +
               $"MemReq: {modelInfo?.MemoryRequirement ?? "Unknown"}";
    }

    /// <summary>
    /// Checks if the current model supports a specific capability
    /// </summary>
    /// <param name="capability">The capability to check</param>
    /// <returns>True if supported</returns>
    public bool SupportsCapability(string capability)
    {
        var modelInfo = GetCurrentModelInfo();
        return modelInfo?.Capabilities?.Contains(capability.ToLowerInvariant()) ?? false;
    }

    /// <summary>
    /// Estimates memory requirements for the current configuration
    /// </summary>
    /// <returns>Memory estimate information</returns>
    public OllamaMemoryEstimate EstimateMemoryRequirements()
    {
        var modelInfo = GetCurrentModelInfo();
        if (modelInfo == null)
        {
            return new OllamaMemoryEstimate { IsValid = false };
        }

        var baseMemoryGb = ParseMemoryRequirement(modelInfo.MemoryRequirement);
        var contextMemoryGb = (ContextSize * 4.0) / (1024 * 1024 * 1024); // Rough estimate
        var totalMemoryGb = baseMemoryGb + contextMemoryGb;

        return new OllamaMemoryEstimate
        {
            IsValid = true,
            BaseModelMemoryGb = baseMemoryGb,
            ContextMemoryGb = contextMemoryGb,
            TotalEstimatedMemoryGb = totalMemoryGb,
            RecommendedSystemMemoryGb = totalMemoryGb * 1.5, // Add 50% buffer
            CanRunOnGpu = GpuLayers > 0 || GpuLayers == -1
        };
    }

    private static double ParseMemoryRequirement(string memReq)
    {
        if (string.IsNullOrEmpty(memReq)) return 0;

        var numPart = new string(memReq.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (double.TryParse(numPart, out var value))
        {
            if (memReq.ToUpperInvariant().Contains("GB"))
                return value;
            if (memReq.ToUpperInvariant().Contains("MB"))
                return value / 1024.0;
        }
        return 0;
    }
}

/// <summary>
/// Information about an Ollama model
/// </summary>
public class OllamaModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public long ParameterCount { get; set; }
    public string MemoryRequirement { get; set; } = string.Empty;
    public int ContextLength { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public string License { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public string DownloadSize { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Ollama server management settings
/// </summary>
public class OllamaServerSettings
{
    /// <summary>
    /// Whether to automatically start Ollama server if not running
    /// </summary>
    public bool AutoStartServer { get; set; } = false;

    /// <summary>
    /// Path to Ollama executable for auto-start
    /// </summary>
    public string? OllamaExecutablePath { get; set; }

    /// <summary>
    /// Server startup timeout in seconds
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Server health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to automatically pull missing models
    /// </summary>
    public bool AutoPullModels { get; set; } = false;

    /// <summary>
    /// Custom environment variables for server
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Validates server settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (StartupTimeoutSeconds <= 0 || StartupTimeoutSeconds > 300)
        {
            errors.Add("StartupTimeoutSeconds must be between 1 and 300");
        }

        if (HealthCheckIntervalSeconds <= 0 || HealthCheckIntervalSeconds > 3600)
        {
            errors.Add("HealthCheckIntervalSeconds must be between 1 and 3600");
        }

        if (AutoStartServer && string.IsNullOrWhiteSpace(OllamaExecutablePath))
        {
            errors.Add("OllamaExecutablePath is required when AutoStartServer is enabled");
        }

        return errors;
    }
}

/// <summary>
/// Performance optimization settings for Ollama
/// </summary>
public class OllamaPerformanceSettings
{
    /// <summary>
    /// Whether to enable CPU optimizations
    /// </summary>
    public bool EnableCpuOptimizations { get; set; } = true;

    /// <summary>
    /// Whether to enable GPU acceleration if available
    /// </summary>
    public bool EnableGpuAcceleration { get; set; } = true;

    /// <summary>
    /// Memory usage limit as percentage of available memory (0-100)
    /// </summary>
    public int MemoryUsagePercentage { get; set; } = 80;

    /// <summary>
    /// Whether to keep model in memory between requests
    /// </summary>
    public bool KeepModelLoaded { get; set; } = true;

    /// <summary>
    /// Model unload timeout in minutes (0 = never unload)
    /// </summary>
    public int ModelUnloadTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to enable response caching
    /// </summary>
    public bool EnableResponseCaching { get; set; } = true;

    /// <summary>
    /// Validates performance settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MemoryUsagePercentage < 10 || MemoryUsagePercentage > 100)
        {
            errors.Add("MemoryUsagePercentage must be between 10 and 100");
        }

        if (ModelUnloadTimeoutMinutes < 0 || ModelUnloadTimeoutMinutes > 1440)
        {
            errors.Add("ModelUnloadTimeoutMinutes must be between 0 and 1440 (24 hours)");
        }

        return errors;
    }
}

/// <summary>
/// Model management settings for Ollama
/// </summary>
public class OllamaModelSettings
{
    /// <summary>
    /// Directory to store downloaded models
    /// </summary>
    public string? ModelDirectory { get; set; }

    /// <summary>
    /// Whether to automatically update models
    /// </summary>
    public bool AutoUpdateModels { get; set; } = false;

    /// <summary>
    /// Update check interval in hours
    /// </summary>
    public int UpdateCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum disk space for models in GB (0 = unlimited)
    /// </summary>
    public double MaxModelStorageGb { get; set; } = 0;

    /// <summary>
    /// Models to preload on startup
    /// </summary>
    public List<string> PreloadModels { get; set; } = new();

    /// <summary>
    /// Validates model settings
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (UpdateCheckIntervalHours < 1 || UpdateCheckIntervalHours > 168)
        {
            errors.Add("UpdateCheckIntervalHours must be between 1 and 168 (1 week)");
        }

        if (MaxModelStorageGb < 0)
        {
            errors.Add("MaxModelStorageGb must be 0 or greater");
        }

        return errors;
    }
}

/// <summary>
/// Privacy and security settings for Ollama
/// </summary>
public class OllamaPrivacySettings
{
    /// <summary>
    /// Whether to enable local-only mode (no external connections)
    /// </summary>
    public bool LocalOnlyMode { get; set; } = true;

    /// <summary>
    /// Whether to log requests and responses
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;

    /// <summary>
    /// Whether to anonymize logged data
    /// </summary>
    public bool AnonymizeLoggedData { get; set; } = true;

    /// <summary>
    /// Log retention period in days
    /// </summary>
    public int LogRetentionDays { get; set; } = 7;

    /// <summary>
    /// Whether to enable telemetry collection
    /// </summary>
    public bool EnableTelemetry { get; set; } = false;

    /// <summary>
    /// Validates privacy settings
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
/// Memory estimation results for Ollama configuration
/// </summary>
public class OllamaMemoryEstimate
{
    public bool IsValid { get; set; }
    public double BaseModelMemoryGb { get; set; }
    public double ContextMemoryGb { get; set; }
    public double TotalEstimatedMemoryGb { get; set; }
    public double RecommendedSystemMemoryGb { get; set; }
    public bool CanRunOnGpu { get; set; }
}
