namespace DevMind.Core.Domain.Entities;

public class UserIntent
{
    public string OriginalRequest { get; private set; } = string.Empty;
    public IntentType Type { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public string? TargetRepository { get; private set; }
    public ConfidenceLevel Confidence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? SessionId { get; private set; }

    private UserIntent() { } // EF Constructor

    public static UserIntent Create(string request, IntentType type,
        Dictionary<string, object>? parameters = null, string? targetRepo = null, Guid? sessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);

        return new UserIntent
        {
            OriginalRequest = request,
            Type = type,
            Parameters = parameters ?? new Dictionary<string, object>(),
            TargetRepository = targetRepo,
            Confidence = ConfidenceLevel.Medium,
            CreatedAt = DateTime.UtcNow,
            SessionId = sessionId
        };
    }

    public void UpdateConfidence(ConfidenceLevel confidence)
    {
        Confidence = confidence;
    }
}

public enum IntentType
{
    Unknown = 0,
    AnalyzeCode,
    CreateBranch,
    RunTests,
    GenerateDocumentation,
    RefactorCode,
    FindBugs,
    OptimizePerformance,
    SecurityScan
}

public enum ConfidenceLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}
