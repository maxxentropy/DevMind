/// <summary>
/// CLI configuration validator service
/// </summary>
public interface ICliConfigurationValidator
{
    Task<List<string>> ValidateAllAsync();
    Task<List<string>> ValidateLlmConfigurationAsync();
    Task<List<string>> ValidateMcpConfigurationAsync();
    Task<List<string>> ValidateInfrastructureConfigurationAsync();
}
