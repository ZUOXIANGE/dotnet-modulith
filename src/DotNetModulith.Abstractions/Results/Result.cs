namespace DotNetModulith.Abstractions.Results;

/// <summary>
/// 操作结果泛型类，封装操作的成功/失败状态及返回值
/// </summary>
/// <typeparam name="T">成功时返回的值类型</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 成功时返回的值
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// 错误代码，用于标识特定错误类型
    /// </summary>
    public string? ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="value">返回值</param>
    public static Result<T> Success(T value) => new(true, value, null, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <param name="code">错误代码</param>
    public static Result<T> Failure(string error, string? code = null) => new(false, default, error, code);

    /// <summary>
    /// 将结果映射为新的类型
    /// </summary>
    /// <typeparam name="TNew">目标类型</typeparam>
    /// <param name="mapper">映射函数</param>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!, ErrorCode);

    /// <summary>
    /// 异步将结果映射为新的类型
    /// </summary>
    /// <typeparam name="TNew">目标类型</typeparam>
    /// <param name="mapper">异步映射函数</param>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper) =>
        IsSuccess ? Result<TNew>.Success(await mapper(Value!)) : Result<TNew>.Failure(Error!, ErrorCode);
}

/// <summary>
/// 操作结果非泛型类，封装操作的成功/失败状态
/// </summary>
public sealed class Result
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// 错误代码，用于标识特定错误类型
    /// </summary>
    public string? ErrorCode { get; }

    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result Success() => new(true, null, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <param name="code">错误代码</param>
    public static Result Failure(string error, string? code = null) => new(false, error, code);
}
