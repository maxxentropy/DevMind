using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevMind.Core.Application.Interfaces;

public interface IPromptService
{
    Task<string> CreateNextStepPromptAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, List<Result<ToolExecution>> history);

    /// <summary>
    /// Creates the prompt used to synthesize a final user-facing response from tool results.
    /// </summary>
    Task<string> CreateSynthesisPromptAsync(UserIntent intent, IEnumerable<ToolExecution> results);
}
