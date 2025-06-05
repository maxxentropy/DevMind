using DevMind.Core.Domain.Entities;

namespace DevMind.Core.Application.Interfaces;

public interface IPlanningService
{
    Task<ExecutionPlan> CreateExecutionPlanAsync(UserIntent intent, CancellationToken cancellationToken = default);
    Task<ExecutionPlan> OptimizePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default);
    Task<bool> ValidatePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default);
}
