namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// Defines the agent's persona, guiding principles, and user-specific context.
/// Used by the PromptService to create context-aware prompts.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// A high-level description of the agent's persona (e.g., "You are an expert C# developer...").
    /// </summary>
    public string Persona { get; set; } = "You are a helpful AI agent.";

    /// <summary>
    /// A list of core rules or principles the agent should always follow.
    /// </summary>
    public List<string> GuidingPrinciples { get; set; } = new();

    /// <summary>
    /// The user's preferred programming language to help the agent make better tool choices.
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// High-level context about the project the user is working on.
    /// </summary>
    public string? ProjectContext { get; set; }
}
