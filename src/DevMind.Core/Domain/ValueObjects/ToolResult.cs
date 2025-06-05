namespace DevMind.Core.Domain.ValueObjects;

public class ToolResult
{
    public ToolCall ToolCall { get; private set; } = null!;
    public bool Success { get; private set; }
    public object? Result { get; private set; }
    public string? Error { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    private ToolResult() { } // EF Constructor

    public static ToolResult Succeeded(ToolCall toolCall, object? result = null, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            ToolCall = toolCall,
            Success = true,
            Result = result,
            CompletedAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    public static ToolResult Failed(ToolCall toolCall, string error)
    {
        return new ToolResult
        {
            ToolCall = toolCall,
            Success = false,
            Error = error,
            CompletedAt = DateTime.UtcNow
        };
    }
}
