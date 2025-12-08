namespace NextAdmin.Shared.Common;

/// <summary>
/// Generic response result
/// </summary>
public class Result
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; protected set; }

    /// <summary>
    /// Whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Create a success result
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Create a failure result
    /// </summary>
    public static Result Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Generic response result with data
/// </summary>
public class Result<T> : Result
{
    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; private set; }

    private Result(bool isSuccess, T? data, string? errorMessage) : base(isSuccess, errorMessage)
    {
        Data = data;
    }

    /// <summary>
    /// Create a success result
    /// </summary>
    public static Result<T> Success(T data) => new(true, data, null);

    /// <summary>
    /// Create a failure result
    /// </summary>
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
} 
