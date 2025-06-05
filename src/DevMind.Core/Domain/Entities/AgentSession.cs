using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Domain.Entities;

public class AgentSession
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public UserRequest Request { get; private set; } = null!;
    public UserIntent Intent { get; private set; } = null!;
    public ExecutionPlan Plan { get; private set; } = null!;
    public AgentResponse Response { get; private set; } = null!;
    public Dictionary<string, object> Context { get; private set; } = new();

    private AgentSession() { } // EF Constructor

    public static AgentSession Create(UserRequest request, UserIntent intent, ExecutionPlan plan, AgentResponse response)
    {
        return new AgentSession
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Request = request ?? throw new ArgumentNullException(nameof(request)),
            Intent = intent ?? throw new ArgumentNullException(nameof(intent)),
            Plan = plan ?? throw new ArgumentNullException(nameof(plan)),
            Response = response ?? throw new ArgumentNullException(nameof(response))
        };
    }
}
