using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DevMind.Core.Application.Services;

public class TaskPlanningService : IPlanningService
{
    // TODO: Implement class members
    
    public TaskPlanningService()
    {
        // TODO: Constructor implementation
    }

    public Task<ExecutionPlan> CreateExecutionPlanAsync(UserIntent intent, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ExecutionPlan> OptimizePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidatePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
