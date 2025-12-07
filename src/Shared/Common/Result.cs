namespace NextAdmin.Shared.Common;

/// <summary>
/// 通用响应结果
/// </summary>
public class Result
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; protected set; }

    /// <summary>
    /// 是否失败
    /// </summary>
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static Result Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// 带数据的通用响应结果
/// </summary>
public class Result<T> : Result
{
    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; private set; }

    private Result(bool isSuccess, T? data, string? errorMessage) : base(isSuccess, errorMessage)
    {
        Data = data;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result<T> Success(T data) => new(true, data, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
} 
