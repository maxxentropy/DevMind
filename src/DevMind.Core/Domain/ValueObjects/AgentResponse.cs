namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// 
/// </summary>
public class AgentResponse
{
    public bool Success { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ResponseType Type { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AgentResponse() { }

    /// <summary>
    /// Creates a successful response with the given content and type.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="type"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a successful response with the given content and type, including metadata.
    /// </summary>
    /// <param name="error"></param>
    /// <param name="originalRequest"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a clarification request response with the given clarification and original request.
    /// </summary>
    /// <param name="clarification"></param>
    /// <param name="originalRequest"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Adds metadata to the response.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public AgentResponse WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}

/// <summary>
/// Represents the type of response from the agent.
/// </summary>
public enum ResponseType
{
    Information = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Clarification = 4
}
