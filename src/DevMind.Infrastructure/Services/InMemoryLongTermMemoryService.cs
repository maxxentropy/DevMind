using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevMind.Infrastructure.Services;

public class InMemoryLongTermMemoryService : ILongTermMemoryService
{
    private readonly ConcurrentDictionary<Guid, List<Result<ToolExecution>>> _sessionStore = new();

    public Task SaveHistoryAsync(Guid sessionId, List<Result<ToolExecution>> history)
    {
        _sessionStore[sessionId] = new List<Result<ToolExecution>>(history);
        return Task.CompletedTask;
    }

    public Task<List<Result<ToolExecution>>> LoadHistoryAsync(Guid sessionId)
    {
        _sessionStore.TryGetValue(sessionId, out var history);
        return Task.FromResult(history ?? new List<Result<ToolExecution>>());
    }
}
