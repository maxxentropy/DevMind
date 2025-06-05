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
    public List<McpToolDefinition> Tools { get; set; } = new();
}
