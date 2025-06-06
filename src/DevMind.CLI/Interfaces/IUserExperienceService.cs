/// <summary>
/// User experience enhancement service
/// </summary>
public interface IUserExperienceService
{
    Task ShowWelcomeAsync();
    Task ShowHelpAsync(string? command = null);
    Task<bool> PromptForConfirmationAsync(string message);
    Task ShowSuccessAsync(string message);
    Task ShowWarningAsync(string message);
}
