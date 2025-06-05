namespace DevMind.Shared.Models;

// External MCP protocol models
public class McpToolRequest
{
    public string Tool { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? SessionId { get; set; }
}

public class McpToolResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class McpToolsResponse
{
    public List<McpToolDto> Tools { get; set; } = new();
}

public class McpToolDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, McpParameterDto> Parameters { get; set; } = new();
}

public class McpParameterDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? Default { get; set; }
}
