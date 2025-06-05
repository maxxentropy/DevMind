namespace DevMind.Shared.Models;

// External tool models for non-MCP integrations
public class ExternalToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ExternalToolParameter> Parameters { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string Example { get; set; } = string.Empty;
}

public class ExternalToolParameter
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<object> AllowedValues { get; set; } = new();
}
