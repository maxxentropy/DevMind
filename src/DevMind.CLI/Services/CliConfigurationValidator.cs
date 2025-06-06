using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Logging;

/// <summary>
/// CLI configuration validation hosted service
/// </summary>
public class CliConfigurationValidator : BackgroundService
{
    private readonly ICliConfigurationValidator _validator;
    private readonly ILogger<CliConfigurationValidator> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public CliConfigurationValidator(
        ICliConfigurationValidator validator,
        ILogger<CliConfigurationValidator> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _validator = validator;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting CLI configuration validation");

            var errors = await _validator.ValidateAllAsync();

            if (errors.Any())
            {
                _logger.LogError("CLI configuration validation failed with {ErrorCount} errors:", errors.Count);
                foreach (var error in errors)
                {
                    _logger.LogError("  - {Error}", error);
                }

                // For CLI applications, configuration errors should be non-fatal in most cases
                // Log the errors but allow the application to continue
                _logger.LogWarning("Continuing with invalid configuration - some features may not work correctly");
            }
            else
            {
                _logger.LogInformation("CLI configuration validation completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CLI configuration validation");
        }
    }
}
