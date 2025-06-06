using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

/// <summary>
/// Enforces constraints and validates inputs and outputs to ensure safe and responsible agent behavior.
/// </summary>
public interface IGuardrailService
{
    /// <summary>
    /// Validates the initial user input.
    /// </summary>
    Task<Result<string>> ValidateInputAsync(string userInput);

    /// <summary>
    /// Validates a tool call proposed by the LLM before it is executed.
    /// </summary>
    Task<Result<bool>> IsActionAllowedAsync(ToolCall toolCall);

    /// <summary>
    /// Validates the agent's final generated response before showing it to the user.
    /// </summary>
    Task<Result<string>> ValidateOutputAsync(string finalResponse);
}
