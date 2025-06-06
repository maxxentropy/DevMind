using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevMind.Core.Domain.ValueObjects;

namespace DevMind.Core.Application.Interfaces;

public interface ILongTermMemoryService
{
    Task SaveHistoryAsync(Guid sessionId, List<Result<ToolExecution>> history);
    Task<List<Result<ToolExecution>>> LoadHistoryAsync(Guid sessionId);
}
