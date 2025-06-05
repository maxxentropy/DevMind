namespace DevMind.Core.Domain.ValueObjects;

public class AgentResponse
{
    public bool Success { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ResponseType Type { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AgentResponse() { }

    public static AgentResponse CreateSuccess(string content, ResponseType type = ResponseType.Information)
    {
        return new AgentResponse
        {
            Success = true,
            Content = content,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AgentResponse CreateError(string error, string? originalRequest = null)
    {
        var response = new AgentResponse
        {
            Success = false,
            Content = "I encountered an error while processing your request.",
            Type = ResponseType.Error,
            Error = error,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(originalRequest))
        {
            response.Metadata["original_request"] = originalRequest;
        }

        return response;
    }

    public static AgentResponse CreateClarificationRequest(string clarification, string originalRequest)
    {
        return new AgentResponse
        {
            Success = true,
            Content = clarification,
            Type = ResponseType.Clarification,
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { ["original_request"] = originalRequest }
        };
    }

    public AgentResponse WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}

public enum ResponseType
{
    Information = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Clarification = 4
}
