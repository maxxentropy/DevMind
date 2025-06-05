namespace DevMind.Core.Domain.ValueObjects;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Implements the Response/Result pattern for better error handling.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    #region Private Fields

    private readonly T? _value;
    private readonly ResultError? _error;

    #endregion

    #region Properties

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value (only available when IsSuccess is true)
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access Value when Result is in failure state");
            return _value!;
        }
    }

    /// <summary>
    /// The error information (only available when IsFailure is true)
    /// </summary>
    public ResultError Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("Cannot access Error when Result is in success state");
            return _error!;
        }
    }

    #endregion

    #region Constructors

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(ResultError error)
    {
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
        IsSuccess = false;
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a successful result with the given value
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A successful result</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    /// <summary>
    /// Creates a failed result with the given error
    /// </summary>
    /// <param name="error">The error information</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(ResultError error)
    {
        return new Result<T>(error);
    }

    /// <summary>
    /// Creates a failed result with error details
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <param name="details">Optional error details</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string code, string message, object? details = null)
    {
        return new Result<T>(ResultError.Create(code, message, details));
    }

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="code">Optional error code (defaults to exception type name)</param>
    /// <returns>A failed result</returns>
    public static Result<T> FromException(Exception exception, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errorCode = code ?? exception.GetType().Name.Replace("Exception", "");
        return new Result<T>(ResultError.FromException(exception, errorCode));
    }

    #endregion

    #region Functional Operations

    /// <summary>
    /// Maps the success value to a new type
    /// </summary>
    /// <typeparam name="TResult">The target type</typeparam>
    /// <param name="mapper">Function to transform the value</param>
    /// <returns>A new result with the transformed value or the same error</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess
            ? Result<TResult>.Success(mapper(Value))
            : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Maps the success value to a new result (flat map/bind operation)
    /// </summary>
    /// <typeparam name="TResult">The target type</typeparam>
    /// <param name="mapper">Function that returns a new result</param>
    /// <returns>The result of the mapper function or the current error</returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess
            ? mapper(Value)
            : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">Action to execute with the success value</param>
    /// <returns>The same result for chaining</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <param name="action">Action to execute with the error</param>
    /// <returns>The same result for chaining</returns>
    public Result<T> OnFailure(Action<ResultError> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    /// <summary>
    /// Returns the success value or a default value if failed
    /// </summary>
    /// <param name="defaultValue">Value to return on failure</param>
    /// <returns>The success value or default value</returns>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? Value : defaultValue;
    }

    /// <summary>
    /// Returns the success value or the result of a function if failed
    /// </summary>
    /// <param name="defaultValueFactory">Function to generate default value</param>
    /// <returns>The success value or generated default value</returns>
    public T GetValueOrDefault(Func<ResultError, T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return IsSuccess ? Value : defaultValueFactory(Error);
    }

  /// <summary>
  /// Converts the result to a nullable value
  /// </summary>
  /// <typeparam name="TStruct">The struct type</typeparam>
  /// <returns>The value if successful, null if failed</returns>
  public TStruct? ToNullable<TStruct>() where TStruct : struct
  {
    if (typeof(T) != typeof(TStruct))
    {
      throw new InvalidOperationException("ToNullable can only be used with the same struct type as the generic type parameter.");
    }

    return IsSuccess ? (TStruct?)(object?)Value : null;
  }

    /// <summary>
    /// Throws an exception if the result is a failure
    /// </summary>
    /// <returns>The success value</returns>
    /// <exception cref="ResultException">Thrown when the result is a failure</exception>
    public T ThrowIfFailure()
    {
        if (IsFailure)
        {
            throw new ResultException(Error);
        }

        return Value;
    }

    #endregion

    #region Implicit Operators

    /// <summary>
    /// Implicit conversion from value to successful result
    /// </summary>
    /// <param name="value">The success value</param>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>
    /// Implicit conversion from error to failed result
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result<T>(ResultError error)
    {
        return Failure(error);
    }

    #endregion

    #region String Representation

    public override string ToString()
    {
        return IsSuccess
            ? $"Success: {Value}"
            : $"Failure: {Error}";
    }

    #endregion
}

/// <summary>
/// Non-generic result for operations that don't return a value
/// </summary>
public class Result
{
    #region Private Fields

    private readonly ResultError? _error;

    #endregion

    #region Properties

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error information (only available when IsFailure is true)
    /// </summary>
    public ResultError Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("Cannot access Error when Result is in success state");
            return _error!;
        }
    }

    #endregion

    #region Constructors

    private Result(bool isSuccess, ResultError? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success()
    {
        return new Result(true);
    }

    /// <summary>
    /// Creates a failed result with the given error
    /// </summary>
    /// <param name="error">The error information</param>
    /// <returns>A failed result</returns>
    public static Result Failure(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with error details
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <param name="details">Optional error details</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string code, string message, object? details = null)
    {
        return new Result(false, ResultError.Create(code, message, details));
    }

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="code">Optional error code (defaults to exception type name)</param>
    /// <returns>A failed result</returns>
    public static Result FromException(Exception exception, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errorCode = code ?? exception.GetType().Name.Replace("Exception", "");
        return new Result(false, ResultError.FromException(exception, errorCode));
    }

    #endregion

    #region Functional Operations

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <returns>The same result for chaining</returns>
    public Result OnSuccess(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <param name="action">Action to execute with the error</param>
    /// <returns>The same result for chaining</returns>
    public Result OnFailure(Action<ResultError> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    /// <summary>
    /// Chains another operation if this result is successful
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <returns>The result of the operation or the current error</returns>
    public Result Bind(Func<Result> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return IsSuccess
            ? operation()
            : this;
    }

    /// <summary>
    /// Throws an exception if the result is a failure
    /// </summary>
    /// <exception cref="ResultException">Thrown when the result is a failure</exception>
    public void ThrowIfFailure()
    {
        if (IsFailure)
        {
            throw new ResultException(Error);
        }
    }

    #endregion

    #region Implicit Operators

    /// <summary>
    /// Implicit conversion from error to failed result
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result(ResultError error)
    {
        return Failure(error);
    }

    #endregion

    #region String Representation

    public override string ToString()
    {
        return IsSuccess
            ? "Success"
            : $"Failure: {Error}";
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Represents detailed error information
/// </summary>
public class ResultError
{
    #region Properties

    /// <summary>
    /// A code identifying the type of error
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Additional error details or context
    /// </summary>
    public object? Details { get; private set; }

    /// <summary>
    /// The original exception if this error was created from one
    /// </summary>
    public Exception? Exception { get; private set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Additional metadata about the error
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; }

    #endregion

    #region Constructors

    private ResultError(string code, string message, object? details = null, Exception? exception = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Details = details;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new error with the specified details
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    /// <param name="details">Optional additional details</param>
    /// <returns>A new ResultError instance</returns>
    public static ResultError Create(string code, string message, object? details = null)
    {
        return new ResultError(code, message, details);
    }

    /// <summary>
    /// Creates an error from an exception
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="code">Optional error code (defaults to exception type)</param>
    /// <returns>A new ResultError instance</returns>
    public static ResultError FromException(Exception exception, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errorCode = code ?? exception.GetType().Name.Replace("Exception", "");
        return new ResultError(errorCode, exception.Message, null, exception);
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Adds metadata to the error
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>The same error for chaining</returns>
    public ResultError WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata entries
    /// </summary>
    /// <param name="metadata">Metadata to add</param>
    /// <returns>The same error for chaining</returns>
    public ResultError WithMetadata(Dictionary<string, object> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        foreach (var kvp in metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }

        return this;
    }

    #endregion

    #region String Representation

    public override string ToString()
    {
        var result = $"[{Code}] {Message}";

        if (Details != null)
        {
            result += $" | Details: {Details}";
        }

        if (Metadata.Any())
        {
            var metadataString = string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            result += $" | Metadata: {metadataString}";
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Exception thrown when calling ThrowIfFailure on a failed result
/// </summary>
public class ResultException : Exception
{
    /// <summary>
    /// The result error that caused this exception
    /// </summary>
    public ResultError ResultError { get; }

    public ResultException(ResultError error)
        : base(error.Message, error.Exception)
    {
        ResultError = error ?? throw new ArgumentNullException(nameof(error));
    }
}

#endregion
