using Microsoft.Extensions.Options;

namespace DevMind.Infrastructure.McpClients;

/// <summary>
/// Validates MCP client options on startup
/// </summary>
public class McpClientOptionsValidator : IValidateOptions<McpClientOptions>
{
    public ValidateOptionsResult Validate(string? name, McpClientOptions options)
    {
        var errors = options.Validate();

        if (errors.Any())
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
