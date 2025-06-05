namespace DevMind.Shared.Models;

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ToolParameter> Parameters { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string Example { get; set; } = string.Empty;
}

public class ToolParameter
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<object> AllowedValues { get; set; } = new();
}
