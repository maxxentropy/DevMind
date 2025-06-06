using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevMind.Infrastructure.Services;

public class PromptService : IPromptService
{
    private readonly UserSettings _userSettings;
    private readonly string _nextStepTemplate;

    public PromptService(IOptions<AgentOptions> agentOptions)
    {
        _userSettings = agentOptions?.Value?.UserSettings ?? new UserSettings();
        _nextStepTemplate = LoadTemplate("NextStepPrompt.txt");
    }

    public Task<string> CreateNextStepPromptAsync(UserIntent intent, IEnumerable<ToolDefinition> availableTools, List<Result<ToolExecution>> history)
    {
        var promptBuilder = new StringBuilder(_nextStepTemplate);

        promptBuilder.Replace("{{persona}}", _userSettings.Persona);
        promptBuilder.Replace("{{guiding_principles}}", string.Join("\n", _userSettings.GuidingPrinciples.Select(p => $"- {p}")));
        promptBuilder.Replace("{{preferred_language}}", _userSettings.PreferredLanguage ?? "Not specified");
        promptBuilder.Replace("{{project_context}}", _userSettings.ProjectContext ?? "Not specified");
        promptBuilder.Replace("{{user_goal}}", intent.OriginalRequest);
        promptBuilder.Replace("{{tool_list}}", string.Join("\n", availableTools.Select(t => $"- {t.Name}: {t.Description}")));
        promptBuilder.Replace("{{history}}", FormatHistoryForPrompt(history));

        return Task.FromResult(promptBuilder.ToString());
    }

    public Task<string> CreateSynthesisPromptAsync(UserIntent intent, IEnumerable<ToolExecution> results)
    {
        var resultsText = results.Any()
            ? string.Join("\n", results.Select(r => $"Tool `{r.ToolCall.ToolName}` output: {r.GetResult<object>()}"))
            : "No tools were executed.";

        return Task.FromResult($@"Based on the user's request ""{intent.OriginalRequest}"" and the following tool results, provide a comprehensive and helpful final answer.
---
TOOL RESULTS:
{resultsText}
---
Final Answer:");
    }

    private string FormatHistoryForPrompt(List<Result<ToolExecution>> history)
    {
        if (!history.Any()) return "No actions taken yet.";

        var historyLog = new StringBuilder();
        foreach (var result in history)
        {
            if (result.IsSuccess)
            {
                var exec = result.Value;
                var toolResult = exec.GetResult<object>() is { } res ? JsonSerializer.Serialize(res) : "[No Output]";
                historyLog.AppendLine($"Action: Executed tool `{exec.ToolCall.ToolName}`. Result: {toolResult}");
            }
            else
            {
                historyLog.AppendLine($"Action: Failed. Error: {result.Error.Code} - {result.Error.Message}");
            }
        }
        return historyLog.ToString();
    }

    private string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"DevMind.Infrastructure.Prompts.{templateName}";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
