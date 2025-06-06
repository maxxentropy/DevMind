using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevMind.Infrastructure.Services;

public class GuardrailService : IGuardrailService
{
    private readonly List<string> _blockedCommands;

    public GuardrailService(IOptions<AgentOptions> agentOptions)
    {
        _blockedCommands = agentOptions?.Value?.Security?.BlockedCommands ?? new List<string>();
    }

    public Task<Result<string>> ValidateInputAsync(string userInput)
    {
        // TODO: Implement input validation (e.g., check for malicious prompts).
        return Task.FromResult(Result<string>.Success(userInput));
    }

    public Task<Result<bool>> IsActionAllowedAsync(ToolCall toolCall)
    {
        if (_blockedCommands.Contains(toolCall.ToolName, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result<bool>.Failure(
                "GUARDRAIL_ACTION_BLOCKED",
                $"Execution of the tool '{toolCall.ToolName}' is blocked by security policy."));
        }
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<string>> ValidateOutputAsync(string finalResponse)
    {
        // TODO: Implement output validation (e.g., check for PII, toxic language).
        return Task.FromResult(Result<string>.Success(finalResponse));
    }
}
