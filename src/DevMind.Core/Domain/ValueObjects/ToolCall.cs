namespace DevMind.Core.Domain.ValueObjects;

public class ToolCall
{
    public string ToolName { get; private set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public Guid? SessionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int Order { get; private set; }

    private ToolCall() { } // EF Constructor

    public static ToolCall Create(string toolName, Dictionary<string, object>? parameters = null, 
        Guid? sessionId = null, int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        
        return new ToolCall
        {
            ToolName = toolName,
            Parameters = parameters ?? new Dictionary<string, object>(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Order = order
        };
    }

    public ToolCall WithParameter(string key, object value)
    {
        Parameters[key] = value;
        return this;
    }
}
