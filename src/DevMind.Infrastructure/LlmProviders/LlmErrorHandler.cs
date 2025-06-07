// src/DevMind.Infrastructure/LlmProviders/LlmErrorHandler.cs

using DevMind.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DevMind.Infrastructure.LlmProviders;

/// <summary>
/// Centralized error handling for LLM operations using the Response pattern
/// </summary>
public class LlmErrorHandler
{
    #region Private Fields

    private readonly ILogger<LlmErrorHandler> _logger;

    #endregion

    #region Constructor

    public LlmErrorHandler(ILogger<LlmErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Handles exceptions from LLM operations and converts them to structured error results
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="provider">The LLM provider name</param>
    /// <param name="operation">The operation being performed</param>
    /// <returns>A structured error result with handling recommendations</returns>
    public Result<LlmErrorHandlingResult> HandleError(Exception exception, string provider, string operation)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        _logger.LogError(exception, "Error in {Provider} during {Operation}: {Message}",
            provider, operation, exception.Message);

        var handlingResult = exception switch
        {
            HttpRequestException httpEx => HandleHttpError(httpEx, provider, operation),
            TimeoutException timeoutEx => HandleTimeoutError(timeoutEx, provider, operation),
            UnauthorizedAccessException authEx => HandleAuthError(authEx, provider, operation),
            ArgumentException argEx => HandleArgumentError(argEx, provider, operation),
            InvalidOperationException opEx => HandleOperationError(opEx, provider, operation),
            _ => HandleGenericError(exception, provider, operation)
        };

        return Result<LlmErrorHandlingResult>.Success(handlingResult);
    }

    /// <summary>
    /// Wraps an LLM operation with comprehensive error handling
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="provider">The LLM provider name</param>
    /// <param name="operationName">The name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing either the operation result or error information</returns>
    public async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<T>> operation,
        string provider,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        try
        {
            _logger.LogDebug("Starting {Operation} for provider {Provider}", operationName, provider);

            var result = await operation();

            _logger.LogDebug("Successfully completed {Operation} for provider {Provider}", operationName, provider);

            return Result<T>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Operation {Operation} was cancelled for provider {Provider}", operationName, provider);

            return Result<T>.Failure(
                LlmErrorCodes.OperationCancelled,
                "The operation was cancelled",
                new { Provider = provider, Operation = operationName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {Operation} for provider {Provider}", operationName, provider);

            var errorHandlingResult = HandleError(ex, provider, operationName);
            var errorInfo = errorHandlingResult.Value;

            return Result<T>.Failure(
                errorInfo.ErrorCode,
                errorInfo.UserMessage,
                new
                {
                    Provider = provider,
                    Operation = operationName,
                    ErrorType = errorInfo.ErrorType,
                    IsRetryable = errorInfo.IsRetryable,
                    RecommendedAction = errorInfo.RecommendedAction,
                    OriginalException = ex.GetType().Name
                });
        }
    }

    /// <summary>
    /// Determines if an error should be retried based on the error type and provider
    /// </summary>
    /// <param name="error">The error to evaluate</param>
    /// <param name="provider">The LLM provider name</param>
    /// <returns>Result indicating whether retry is recommended</returns>
    public Result<bool> ShouldRetry(ResultError error, string provider)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        try
        {
            var shouldRetry = error.Code switch
            {
                LlmErrorCodes.RateLimit or
                LlmErrorCodes.ServiceUnavailable or
                LlmErrorCodes.Timeout or
                LlmErrorCodes.NetworkError => true,

                LlmErrorCodes.Authentication or
                LlmErrorCodes.InvalidRequest or
                LlmErrorCodes.Configuration => false,

                _ => false
            };

            return Result<bool>.Success(shouldRetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining retry logic for error {ErrorCode}", error.Code);
            return Result<bool>.Failure("RetryEvaluationFailed", "Could not determine if operation should be retried");
        }
    }

    /// <summary>
    /// Calculates the recommended delay before retry
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="attemptNumber">Current attempt number (1-based)</param>
    /// <param name="provider">The LLM provider name</param>
    /// <returns>Result containing the recommended delay</returns>
    public Result<TimeSpan> GetRetryDelay(ResultError error, int attemptNumber, string provider)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        if (attemptNumber <= 0)
        {
            return Result<TimeSpan>.Failure(
                LlmErrorCodes.InvalidRequest,
                "Attempt number must be positive");
        }

        try
        {
            // Base delay with exponential backoff
            var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber - 1));

            // Provider-specific adjustments
            var providerMultiplier = provider.ToLowerInvariant() switch
            {
                "openai" => 1.0,
                "anthropic" => 1.2,
                "ollama" => 0.5,
                "azure-openai" => 1.1,
                _ => 1.0
            };

            // Error-type specific adjustments
            var errorMultiplier = error.Code switch
            {
                LlmErrorCodes.RateLimit => 2.0,
                LlmErrorCodes.Timeout => 1.5,
                _ => 1.0
            };

            var finalDelay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * providerMultiplier * errorMultiplier);

            // Cap at maximum delay
            var cappedDelay = finalDelay > TimeSpan.FromMinutes(2)
                ? TimeSpan.FromMinutes(2)
                : finalDelay;

            return Result<TimeSpan>.Success(cappedDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating retry delay");
            return Result<TimeSpan>.Failure(
                LlmErrorCodes.Configuration,
                "Could not calculate retry delay");
        }
    }

    #endregion

    #region Private Methods

    private LlmErrorHandlingResult HandleHttpError(HttpRequestException exception, string provider, string operation)
    {
        // Use the status code directly from the exception
        var statusCode = exception.StatusCode;

        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
                return new LlmErrorHandlingResult
                {
                    ErrorCode = LlmErrorCodes.Authentication,
                    ErrorType = LlmErrorType.Authentication,
                    IsRetryable = false,
                    RecommendedAction = "Check API key configuration",
                    UserMessage = "Authentication failed. Please verify your API credentials.",
                    ProviderSpecificData = new { StatusCode = statusCode, Provider = provider }
                };
            case HttpStatusCode.TooManyRequests:
                return new LlmErrorHandlingResult
                {
                    ErrorCode = LlmErrorCodes.RateLimit,
                    ErrorType = LlmErrorType.RateLimit,
                    IsRetryable = true,
                    RecommendedAction = "Implement exponential backoff",
                    UserMessage = "The API rate limit has been exceeded. The request will be retried automatically.",
                    ProviderSpecificData = new { StatusCode = statusCode, Provider = provider }
                };
            case HttpStatusCode.BadRequest:
                return new LlmErrorHandlingResult
                {
                    ErrorCode = LlmErrorCodes.InvalidRequest,
                    ErrorType = LlmErrorType.InvalidRequest,
                    IsRetryable = false,
                    RecommendedAction = "Review request parameters",
                    UserMessage = "Invalid request format. Please check your input.",
                    ProviderSpecificData = new { StatusCode = statusCode, Provider = provider }
                };
            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                return new LlmErrorHandlingResult
                {
                    ErrorCode = LlmErrorCodes.ServiceUnavailable,
                    ErrorType = LlmErrorType.ServiceUnavailable,
                    IsRetryable = true,
                    RecommendedAction = "Retry with backoff",
                    UserMessage = "The remote service is temporarily unavailable. Retrying automatically.",
                    ProviderSpecificData = new { StatusCode = statusCode, Provider = provider }
                };
            default:
                return new LlmErrorHandlingResult
                {
                    ErrorCode = LlmErrorCodes.Unknown,
                    ErrorType = LlmErrorType.Unknown,
                    IsRetryable = false,
                    RecommendedAction = "Log for investigation",
                    UserMessage = $"An unexpected HTTP error occurred: {statusCode}",
                    ProviderSpecificData = new { StatusCode = statusCode, Provider = provider }
                };
        }
    }
    private LlmErrorHandlingResult HandleTimeoutError(TimeoutException exception, string provider, string operation)
    {
        return new LlmErrorHandlingResult
        {
            ErrorCode = LlmErrorCodes.Timeout,
            ErrorType = LlmErrorType.Timeout,
            IsRetryable = true,
            RecommendedAction = "Increase timeout or retry",
            UserMessage = "Request timed out. Retrying with extended timeout.",
            ProviderSpecificData = new { Provider = provider, Operation = operation }
        };
    }

    private LlmErrorHandlingResult HandleAuthError(UnauthorizedAccessException exception, string provider, string operation)
    {
        return new LlmErrorHandlingResult
        {
            ErrorCode = LlmErrorCodes.Authentication,
            ErrorType = LlmErrorType.Authentication,
            IsRetryable = false,
            RecommendedAction = "Check credentials and permissions",
            UserMessage = "Access denied. Please verify your credentials.",
            ProviderSpecificData = new { Provider = provider, Operation = operation }
        };
    }

    private LlmErrorHandlingResult HandleArgumentError(ArgumentException exception, string provider, string operation)
    {
        return new LlmErrorHandlingResult
        {
            ErrorCode = LlmErrorCodes.InvalidRequest,
            ErrorType = LlmErrorType.InvalidRequest,
            IsRetryable = false,
            RecommendedAction = "Fix request parameters",
            UserMessage = "Invalid request parameters. Please check your input.",
            ProviderSpecificData = new { Provider = provider, Operation = operation, Parameter = exception.ParamName }
        };
    }

    private LlmErrorHandlingResult HandleOperationError(InvalidOperationException exception, string provider, string operation)
    {
        return new LlmErrorHandlingResult
        {
            ErrorCode = LlmErrorCodes.Configuration,
            ErrorType = LlmErrorType.Configuration,
            IsRetryable = false,
            RecommendedAction = "Check service configuration",
            UserMessage = "Service configuration error. Please contact support.",
            ProviderSpecificData = new { Provider = provider, Operation = operation }
        };
    }

    private LlmErrorHandlingResult HandleGenericError(Exception exception, string provider, string operation)
    {
        return new LlmErrorHandlingResult
        {
            ErrorCode = LlmErrorCodes.Unknown,
            ErrorType = LlmErrorType.Unknown,
            IsRetryable = false,
            RecommendedAction = "Log for investigation",
            UserMessage = "An unexpected error occurred. Please try again later.",
            ProviderSpecificData = new
            {
                Provider = provider,
                Operation = operation,
                ExceptionType = exception.GetType().Name
            }
        };
    }

    private static HttpStatusCode? ExtractStatusCode(HttpRequestException exception)
    {
        // Try to extract status code from exception message or data
        // This is a simplified implementation - actual implementation would depend on
        // how HttpRequestException is being used in your HTTP clients

        var message = exception.Message.ToLowerInvariant();

        return message switch
        {
            var msg when msg.Contains("401") || msg.Contains("unauthorized") => HttpStatusCode.Unauthorized,
            var msg when msg.Contains("429") || msg.Contains("too many requests") => HttpStatusCode.TooManyRequests,
            var msg when msg.Contains("400") || msg.Contains("bad request") => HttpStatusCode.BadRequest,
            var msg when msg.Contains("500") || msg.Contains("internal server error") => HttpStatusCode.InternalServerError,
            var msg when msg.Contains("502") || msg.Contains("bad gateway") => HttpStatusCode.BadGateway,
            var msg when msg.Contains("503") || msg.Contains("service unavailable") => HttpStatusCode.ServiceUnavailable,
            var msg when msg.Contains("504") || msg.Contains("gateway timeout") => HttpStatusCode.GatewayTimeout,
            _ => null
        };
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Result of error handling analysis using the Response pattern
/// </summary>
public class LlmErrorHandlingResult
{
    /// <summary>
    /// Standardized error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// The categorized type of error
    /// </summary>
    public LlmErrorType ErrorType { get; set; }

    /// <summary>
    /// Whether this error type should be retried
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Recommended action for handling this error
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly error message
    /// </summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific error data
    /// </summary>
    public object? ProviderSpecificData { get; set; }

    /// <summary>
    /// Additional metadata about the error
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// When the error was handled
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Categories of LLM errors for handling logic
/// </summary>
public enum LlmErrorType
{
    /// <summary>
    /// Authentication or authorization error
    /// </summary>
    Authentication,

    /// <summary>
    /// Rate limiting error
    /// </summary>
    RateLimit,

    /// <summary>
    /// Invalid request format or parameters
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// Service temporarily unavailable
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// Request timeout
    /// </summary>
    Timeout,

    /// <summary>
    /// Configuration or setup error
    /// </summary>
    Configuration,

    /// <summary>
    /// Network connectivity error
    /// </summary>
    Network,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Unknown or unhandled error
    /// </summary>
    Unknown
}

/// <summary>
/// Standardized error codes for LLM operations
/// </summary>
public static class LlmErrorCodes
{
    public const string Authentication = "LLM_AUTH_FAILED";
    public const string RateLimit = "LLM_RATE_LIMIT";
    public const string InvalidRequest = "LLM_INVALID_REQUEST";
    public const string ServiceUnavailable = "LLM_SERVICE_UNAVAILABLE";
    public const string Timeout = "LLM_TIMEOUT";
    public const string Configuration = "LLM_CONFIG_ERROR";
    public const string NetworkError = "LLM_NETWORK_ERROR";
    public const string OperationCancelled = "LLM_OPERATION_CANCELLED";
    public const string Unknown = "LLM_UNKNOWN_ERROR";
    public const string ProviderNotAvailable = "LLM_PROVIDER_UNAVAILABLE";
    public const string ModelNotFound = "LLM_MODEL_NOT_FOUND";
    public const string QuotaExceeded = "LLM_QUOTA_EXCEEDED";
    public const string ContentFiltered = "LLM_CONTENT_FILTERED";
}

#endregion
