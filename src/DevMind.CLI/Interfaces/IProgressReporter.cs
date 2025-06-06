/// <summary>
/// Progress reporting service for CLI operations
/// </summary>
public interface IProgressReporter
{
    Task StartAsync(string operation);
    Task UpdateAsync(string status, int? percentage = null);
    Task CompleteAsync(string finalStatus);
    Task FailAsync(string error);
}
