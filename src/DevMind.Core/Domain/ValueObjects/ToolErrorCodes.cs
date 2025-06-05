// src/DevMind.Core/Domain/ValueObjects/ToolErrorCodes.cs

namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// Standardized error codes for tool execution operations
/// </summary>
public static class ToolErrorCodes
{
    #region Execution Errors

    /// <summary>
    /// General tool execution failure
    /// </summary>
    public const string ExecutionFailed = "TOOL_EXECUTION_FAILED";

    /// <summary>
    /// Tool execution exceeded timeout limit
    /// </summary>
    public const string ExecutionTimeout = "TOOL_EXECUTION_TIMEOUT";

    /// <summary>
    /// Tool execution was cancelled by user or system
    /// </summary>
    public const string ExecutionCancelled = "TOOL_EXECUTION_CANCELLED";

    #endregion

    #region Parameter and Input Errors

    /// <summary>
    /// Invalid or missing required parameters
    /// </summary>
    public const string InvalidParameters = "TOOL_INVALID_PARAMETERS";

    /// <summary>
    /// Parameter validation failed
    /// </summary>
    public const string ParameterValidationFailed = "TOOL_PARAMETER_VALIDATION_FAILED";

    /// <summary>
    /// Required parameter is missing
    /// </summary>
    public const string MissingRequiredParameter = "TOOL_MISSING_REQUIRED_PARAMETER";

    #endregion

    #region Access and Security Errors

    /// <summary>
    /// Access denied to execute tool or access resources
    /// </summary>
    public const string AccessDenied = "TOOL_ACCESS_DENIED";

    /// <summary>
    /// Insufficient permissions to execute tool
    /// </summary>
    public const string InsufficientPermissions = "TOOL_INSUFFICIENT_PERMISSIONS";

    /// <summary>
    /// Authentication required for tool execution
    /// </summary>
    public const string AuthenticationRequired = "TOOL_AUTHENTICATION_REQUIRED";

    #endregion

    #region Resource Errors

    /// <summary>
    /// Required resource not found
    /// </summary>
    public const string ResourceNotFound = "TOOL_RESOURCE_NOT_FOUND";

    /// <summary>
    /// Resource is currently unavailable
    /// </summary>
    public const string ResourceUnavailable = "TOOL_RESOURCE_UNAVAILABLE";

    /// <summary>
    /// Resource quota or limit exceeded
    /// </summary>
    public const string ResourceLimitExceeded = "TOOL_RESOURCE_LIMIT_EXCEEDED";

    #endregion

    #region Tool State Errors

    /// <summary>
    /// Requested tool not found or does not exist
    /// </summary>
    public const string ToolNotFound = "TOOL_NOT_FOUND";

    /// <summary>
    /// Tool is currently unavailable for execution
    /// </summary>
    public const string ToolUnavailable = "TOOL_UNAVAILABLE";

    /// <summary>
    /// Tool is disabled or maintenance mode
    /// </summary>
    public const string ToolDisabled = "TOOL_DISABLED";

    /// <summary>
    /// Tool version mismatch or incompatibility
    /// </summary>
    public const string ToolVersionMismatch = "TOOL_VERSION_MISMATCH";

    #endregion

    #region Configuration Errors

    /// <summary>
    /// Tool configuration error
    /// </summary>
    public const string ConfigurationError = "TOOL_CONFIGURATION_ERROR";

    /// <summary>
    /// Missing required configuration for tool
    /// </summary>
    public const string MissingConfiguration = "TOOL_MISSING_CONFIGURATION";

    /// <summary>
    /// Invalid tool configuration
    /// </summary>
    public const string InvalidConfiguration = "TOOL_INVALID_CONFIGURATION";

    #endregion

    #region Network and Communication Errors

    /// <summary>
    /// Network error during tool execution
    /// </summary>
    public const string NetworkError = "TOOL_NETWORK_ERROR";

    /// <summary>
    /// Connection timeout to tool service
    /// </summary>
    public const string ConnectionTimeout = "TOOL_CONNECTION_TIMEOUT";

    /// <summary>
    /// Service unavailable
    /// </summary>
    public const string ServiceUnavailable = "TOOL_SERVICE_UNAVAILABLE";

    #endregion

    #region Operational Errors

    /// <summary>
    /// Invalid operation for current tool state
    /// </summary>
    public const string InvalidOperation = "TOOL_INVALID_OPERATION";

    /// <summary>
    /// Tool operation not supported
    /// </summary>
    public const string OperationNotSupported = "TOOL_OPERATION_NOT_SUPPORTED";

    /// <summary>
    /// Concurrent execution limit reached
    /// </summary>
    public const string ConcurrencyLimitReached = "TOOL_CONCURRENCY_LIMIT_REACHED";

    #endregion

    #region Data and Format Errors

    /// <summary>
    /// Data format error in tool input or output
    /// </summary>
    public const string DataFormatError = "TOOL_DATA_FORMAT_ERROR";

    /// <summary>
    /// Data corruption detected
    /// </summary>
    public const string DataCorruption = "TOOL_DATA_CORRUPTION";

    /// <summary>
    /// Unsupported data format
    /// </summary>
    public const string UnsupportedDataFormat = "TOOL_UNSUPPORTED_DATA_FORMAT";

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a user-friendly description for an error code
    /// </summary>
    /// <param name="errorCode">The error code to describe</param>
    /// <returns>A human-readable description of the error</returns>
    public static string GetDescription(string errorCode)
    {
        return errorCode switch
        {
            ExecutionFailed => "Tool execution failed due to an unexpected error",
            ExecutionTimeout => "Tool execution exceeded the allowed time limit",
            ExecutionCancelled => "Tool execution was cancelled",

            InvalidParameters => "The provided parameters are invalid or incorrectly formatted",
            ParameterValidationFailed => "Parameter validation failed according to tool requirements",
            MissingRequiredParameter => "A required parameter is missing from the request",

            AccessDenied => "Access denied - insufficient permissions to execute this tool",
            InsufficientPermissions => "Your account lacks the necessary permissions for this operation",
            AuthenticationRequired => "Authentication is required to execute this tool",

            ResourceNotFound => "The requested resource could not be found",
            ResourceUnavailable => "The required resource is temporarily unavailable",
            ResourceLimitExceeded => "Resource usage limit has been exceeded",

            ToolNotFound => "The requested tool does not exist or is not available",
            ToolUnavailable => "The tool is currently unavailable for execution",
            ToolDisabled => "The tool is disabled or in maintenance mode",
            ToolVersionMismatch => "Tool version is incompatible with the current system",

            ConfigurationError => "Tool configuration error prevents execution",
            MissingConfiguration => "Required configuration is missing for this tool",
            InvalidConfiguration => "Tool configuration is invalid or corrupted",

            NetworkError => "A network error occurred during tool execution",
            ConnectionTimeout => "Connection to the tool service timed out",
            ServiceUnavailable => "The tool service is currently unavailable",

            InvalidOperation => "The requested operation is invalid for the current tool state",
            OperationNotSupported => "This operation is not supported by the tool",
            ConcurrencyLimitReached => "Maximum concurrent executions reached",

            DataFormatError => "Data format error in tool input or output",
            DataCorruption => "Data corruption was detected during processing",
            UnsupportedDataFormat => "The data format is not supported by this tool",

            _ => "An unknown error occurred during tool execution"
        };
    }

    /// <summary>
    /// Determines if an error code represents a retryable error
    /// </summary>
    /// <param name="errorCode">The error code to check</param>
    /// <returns>True if the error is potentially retryable</returns>
    public static bool IsRetryable(string errorCode)
    {
        return errorCode switch
        {
            ExecutionTimeout or
            NetworkError or
            ConnectionTimeout or
            ServiceUnavailable or
            ResourceUnavailable or
            ToolUnavailable or
            ConcurrencyLimitReached => true,

            _ => false
        };
    }

    /// <summary>
    /// Gets the category of an error code
    /// </summary>
    /// <param name="errorCode">The error code to categorize</param>
    /// <returns>The error category</returns>
    public static ToolErrorCategory GetCategory(string errorCode)
    {
        return errorCode switch
        {
            ExecutionFailed or ExecutionTimeout or ExecutionCancelled => ToolErrorCategory.Execution,

            InvalidParameters or ParameterValidationFailed or MissingRequiredParameter => ToolErrorCategory.Parameters,

            AccessDenied or InsufficientPermissions or AuthenticationRequired => ToolErrorCategory.Security,

            ResourceNotFound or ResourceUnavailable or ResourceLimitExceeded => ToolErrorCategory.Resource,

            ToolNotFound or ToolUnavailable or ToolDisabled or ToolVersionMismatch => ToolErrorCategory.Tool,

            ConfigurationError or MissingConfiguration or InvalidConfiguration => ToolErrorCategory.Configuration,

            NetworkError or ConnectionTimeout or ServiceUnavailable => ToolErrorCategory.Network,

            InvalidOperation or OperationNotSupported or ConcurrencyLimitReached => ToolErrorCategory.Operation,

            DataFormatError or DataCorruption or UnsupportedDataFormat => ToolErrorCategory.Data,

            _ => ToolErrorCategory.Unknown
        };
    }

    #endregion
}

/// <summary>
/// Categories of tool errors for classification and handling
/// </summary>
public enum ToolErrorCategory
{
    /// <summary>
    /// Execution-related errors
    /// </summary>
    Execution,

    /// <summary>
    /// Parameter and input validation errors
    /// </summary>
    Parameters,

    /// <summary>
    /// Security and access control errors
    /// </summary>
    Security,

    /// <summary>
    /// Resource availability and management errors
    /// </summary>
    Resource,

    /// <summary>
    /// Tool availability and state errors
    /// </summary>
    Tool,

    /// <summary>
    /// Configuration and setup errors
    /// </summary>
    Configuration,

    /// <summary>
    /// Network and communication errors
    /// </summary>
    Network,

    /// <summary>
    /// Operational and state management errors
    /// </summary>
    Operation,

    /// <summary>
    /// Data format and processing errors
    /// </summary>
    Data,

    /// <summary>
    /// Unknown or uncategorized errors
    /// </summary>
    Unknown
}
