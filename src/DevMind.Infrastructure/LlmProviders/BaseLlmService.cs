// src/DevMind.Infrastructure/LlmProviders/BaseLlmService.cs

using DevMind.Core.Application.Interfaces;
using DevMind.Core.Domain.Entities;
using DevMind.Core.Domain.ValueObjects;
using DevMind.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevMind.Infrastructure.LlmProviders;

/// <summary>
/// Base class for LLM service implementations providing common error handling
/// and response pattern implementation
/// </summary>
public abstract class BaseLlmService : ILlmService
{
    #region Protected Fields

    protected readonly ILogger _logger;
    protected readonly LlmErrorHandler _errorHandler;

    #endregion

    #region Constructor

    protected BaseLlmService(ILogger logger, LlmErrorHandler errorHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    #endregion

    #region Abstract Properties

    /// <summary>
    /// The name of the LLM provider (e.g., "openai", "anthropic")
    /// </summary>
    protected abstract string ProviderName { get; }

    #endregion

    #region ILlmService Implementation

    /// <summary>
    /// Analyzes user intent using the LLM provider
    /// </summary>
    /// <param name="request">The user request to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the analyzed user intent or error information</returns>
    public async Task<Result<UserIntent>> AnalyzeIntentAsync(UserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _errorHandler.ExecuteWithErrorHandling(
            async () => await AnalyzeIntentInternalAsync(request, cancellationToken),
            ProviderName,
            "AnalyzeIntent",
            cancellationToken);
    }

    /// <summary>
    /// Creates an execution plan using the LLM provider
    /// </summary>
    /// <param name="intent">The user intent to plan for</param>
    /// <param name="availableTools">Available tools for the plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the execution plan or error information</returns>
    public async Task<Result<ExecutionPlan>> CreateExecutionPlanAsync(
        UserIntent intent,
        IEnumerable<ToolDefinition> availableTools,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(availableTools);

        return await _errorHandler.ExecuteWithErrorHandling(
            async () => await CreateExecutionPlanInternalAsync(intent, availableTools, cancellationToken),
            ProviderName,
            "CreateExecutionPlan",
            cancellationToken);
    }

    /// <summary>
    /// Synthesizes a response using the LLM provider
    /// </summary>
    /// <param name="intent">The original user intent</param>
    /// <param name="plan">The execution plan that was followed</param>
    /// <param name="results">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the synthesized response or error information</returns>
    public async Task<Result<string>> SynthesizeResponseAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolResult> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(results);

        return await _errorHandler.ExecuteWithErrorHandling(
            async () => await SynthesizeResponseInternalAsync(intent, plan, results, cancellationToken),
            ProviderName,
            "SynthesizeResponse",
            cancellationToken);
    }

    /// <summary>
    /// Generates a response for a given prompt
    /// </summary>
    /// <param name="prompt">The prompt to generate a response for</param>
    /// <param name="options">Optional LLM options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the generated response or error information</returns>
    public async Task<Result<string>> GenerateResponseAsync(
        string prompt,
        LlmOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        return await _errorHandler.ExecuteWithErrorHandling(
            async () => await GenerateResponseInternalAsync(prompt, options ?? LlmOptions.Default, cancellationToken),
            ProviderName,
            "GenerateResponse",
            cancellationToken);
    }

    /// <summary>
    /// Performs a health check on the LLM provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating whether the health check passed</returns>
    public async Task<Result<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return await _errorHandler.ExecuteWithErrorHandling(
            async () => await HealthCheckInternalAsync(cancellationToken),
            ProviderName,
            "HealthCheck",
            cancellationToken);
    }

    #endregion

    #region Abstract Methods - Provider Implementation

    /// <summary>
    /// Provider-specific implementation of intent analysis
    /// </summary>
    /// <param name="request">The user request to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The analyzed user intent</returns>
    protected abstract Task<UserIntent> AnalyzeIntentInternalAsync(UserRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Provider-specific implementation of execution plan creation
    /// </summary>
    /// <param name="intent">The user intent to plan for</param>
    /// <param name="availableTools">Available tools for the plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution plan</returns>
    protected abstract Task<ExecutionPlan> CreateExecutionPlanInternalAsync(
        UserIntent intent,
        IEnumerable<ToolDefinition> availableTools,
        CancellationToken cancellationToken);

    /// <summary>
    /// Provider-specific implementation of response synthesis
    /// </summary>
    /// <param name="intent">The original user intent</param>
    /// <param name="plan">The execution plan that was followed</param>
    /// <param name="results">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The synthesized response</returns>
    protected abstract Task<string> SynthesizeResponseInternalAsync(
        UserIntent intent,
        ExecutionPlan plan,
        IEnumerable<ToolResult> results,
        CancellationToken cancellationToken);

    /// <summary>
    /// Provider-specific implementation of response generation
    /// </summary>
    /// <param name="prompt">The prompt to generate a response for</param>
    /// <param name="options">LLM options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated response</returns>
    protected abstract Task<string> GenerateResponseInternalAsync(
        string prompt,
        LlmOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Provider-specific implementation of health check
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy, false otherwise</returns>
    protected abstract Task<bool> HealthCheckInternalAsync(CancellationToken cancellationToken);

    #endregion

    #region Protected Helper Methods

    /// <summary>
    /// Executes an operation with retry logic based on error analysis
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    protected async Task<Result<T>> ExecuteWithRetry<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var attempt = 1;

        while (attempt <= maxRetries + 1) // +1 for initial attempt
        {
            var result = await _errorHandler.ExecuteWithErrorHandling(
                operation,
                ProviderName,
                operationName,
                cancellationToken);

            if (result.IsSuccess)
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("Operation {Operation} succeeded on attempt {Attempt}",
                        operationName, attempt);
                }
                return result;
            }

            // Check if we should retry
            var shouldRetryResult = _errorHandler.ShouldRetry(result.Error, ProviderName);
            if (shouldRetryResult.IsFailure || !shouldRetryResult.Value || attempt > maxRetries)
            {
                _logger.LogWarning("Operation {Operation} failed after {Attempt} attempts. Final error: {Error}",
                    operationName, attempt, result.Error.Message);
                return result;
            }

            // Calculate delay before retry
            var delayResult = _errorHandler.GetRetryDelay(result.Error, attempt, ProviderName);
            var delay = delayResult.IsSuccess ? delayResult.Value : TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

            _logger.LogInformation("Operation {Operation} failed on attempt {Attempt}, retrying in {Delay}ms. Error: {Error}",
                operationName, attempt, delay.TotalMilliseconds, result.Error.Message);

            await Task.Delay(delay, cancellationToken);
            attempt++;
        }

        // This should never be reached due to the loop logic, but included for completeness
        return Result<T>.Failure(LlmErrorCodes.Unknown, "Unexpected error in retry logic");
    }

    /// <summary>
    /// Validates that the service is properly configured
    /// </summary>
    /// <returns>Result indicating if configuration is valid</returns>
    protected virtual Result ValidateConfiguration()
    {
        // Base implementation - override in derived classes for provider-specific validation
        return Result.Success();
    }

    /// <summary>
    /// Logs performance metrics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="duration">How long the operation took</param>
    /// <param name="success">Whether the operation was successful</param>
    protected virtual void LogPerformanceMetrics(string operationName, TimeSpan duration, bool success)
    {
        _logger.LogInformation("LLM Operation: {Provider}.{Operation} completed in {Duration}ms (Success: {Success})",
            ProviderName, operationName, duration.TotalMilliseconds, success);
    }

    #endregion
}

// Example concrete implementation showing how to use the base class
// src/DevMind.Infrastructure/LlmProviders/OpenAiService.cs (Updated)

