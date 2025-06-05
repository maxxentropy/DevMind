namespace DevMind.Core.Domain.ValueObjects;

public class UserRequest
{
    public string Content { get; private set; } = string.Empty;
    public string WorkingDirectory { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public Dictionary<string, object> Context { get; private set; } = new();
    public Guid? SessionId { get; private set; }

    private UserRequest() { }

    public static UserRequest Create(string content, string? workingDirectory = null, Guid? sessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        
        return new UserRequest
        {
            Content = content,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId
        };
    }

    public UserRequest WithContext(Dictionary<string, object> context)
    {
        Context = context ?? new Dictionary<string, object>();
        return this;
    }
}
