using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Domain.Entities;

public class ExecutionPlan
{
    private readonly List<ToolCall> _steps = new();

    public Guid Id { get; private set; }
    public UserIntent Intent { get; private set; } = null!;
    public IReadOnlyList<ToolCall> Steps => _steps.AsReadOnly();
    public ExecutionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? EstimatedDuration { get; private set; }

    private ExecutionPlan() { } // EF Constructor

    public static ExecutionPlan Create(UserIntent intent, string? estimatedDuration = null)
    {
        ArgumentNullException.ThrowIfNull(intent);
        
        return new ExecutionPlan
        {
            Id = Guid.NewGuid(),
            Intent = intent,
            Status = ExecutionStatus.Created,
            CreatedAt = DateTime.UtcNow,
            EstimatedDuration = estimatedDuration
        };
    }

    public void AddStep(ToolCall toolCall)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        
        if (Status != ExecutionStatus.Created && Status != ExecutionStatus.Planning)
            throw new InvalidOperationException("Cannot add steps to plan that is already executing");

        _steps.Add(toolCall);
    }

    public void StartExecution()
    {
        if (Status != ExecutionStatus.Created)
            throw new InvalidOperationException("Plan has already been started");

        Status = ExecutionStatus.Executing;
    }

    public void CompleteExecution()
    {
        if (Status != ExecutionStatus.Executing)
            throw new InvalidOperationException("Plan is not currently executing");

        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void FailExecution(string reason)
    {
        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum ExecutionStatus
{
    Created = 0,
    Planning = 1,
    Executing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
