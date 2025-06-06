using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Infrastructure.Configuration;

/// <summary>
/// Binds to the "Agent" section of the application configuration.
/// Contains settings related to the agent's core behavior, user preferences, and security policies.
/// </summary>
public class AgentOptions
{
    /// <summary>
    /// Holds personalized settings and context for the agent's persona and goals.
    /// Maps to the "Agent:UserSettings" configuration section.
    /// </summary>
    public UserSettings UserSettings { get; set; } = new();

    /// <summary>
    /// The default directory where the agent should perform file-based operations.
    /// </summary>
    public string DefaultWorkingDirectory { get; set; } = ".";

    /// <summary>
    /// A timeout for the entire agent execution pipeline for a single request.
    /// </summary>
    public int MaxExecutionTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// The maximum number of tools that can be executed in parallel.
    /// </summary>
    public int MaxConcurrentToolExecutions { get; set; } = 3;

    /// <summary>
    //  Whether the agent should persist context between sessions.
    /// </summary>
    public bool EnableContextPersistence { get; set; } = true;

    /// <summary>
    /// Holds security-related configurations, such as guardrails and access restrictions.
    /// Maps to the "Agent:Security" configuration section.
    /// </summary>
    public AgentSecurityOptions Security { get; set; } = new();
}

/// <summary>
/// Defines security policies and restrictions for the agent.
/// Used by the GuardrailService to validate agent actions.
/// </summary>
public class AgentSecurityOptions
{
    /// <summary>
    /// Whether to enable sanitization of user input.
    /// </summary>
    public bool EnableInputSanitization { get; set; } = true;

    /// <summary>
    /// Whether to restrict tool access to the file system.
    /// </summary>
    public bool RestrictFileSystemAccess { get; set; } = true;

    /// <summary>
    /// A list of directories that the agent is allowed to access.
    /// </summary>
    public List<string> AllowedDirectories { get; set; } = new();

    /// <summary>
    /// A denylist of specific tools or commands that the agent is never allowed to execute.
    /// </summary>
    public List<string> BlockedCommands { get; set; } = new();
}
