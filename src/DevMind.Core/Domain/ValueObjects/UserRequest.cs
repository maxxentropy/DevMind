using System;
using System.Collections.Generic;

namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// Represents a user's request to the agent.
/// Defined as a record for immutability and value-based equality.
/// </summary>
public record UserRequest
{
    public string Content { get; init; } = string.Empty;
    public string WorkingDirectory { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
    public Guid? SessionId { get; init; }

    // Private constructor for the factory method
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
}
