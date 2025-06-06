/// <summary>
/// User-friendly logging service for CLI
/// </summary>
public interface IUserFriendlyLogger
{
    Task LogSuccessAsync(string message);
    Task LogWarningAsync(string message);
    Task LogErrorAsync(string message);
    Task LogInfoAsync(string message);
    Task LogDebugAsync(string message);
}
