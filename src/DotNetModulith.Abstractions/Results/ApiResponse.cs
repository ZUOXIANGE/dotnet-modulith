namespace DotNetModulith.Abstractions.Results;

/// <summary>
/// 统一 API 返回结构
/// </summary>
public sealed record ApiResponse<T>(string Msg, int Code, T? Data);

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T? data, string msg = "success", int code = ApiCodes.Common.Success)
        => new(msg, code, data);

    public static ApiResponse<object?> Success(string msg = "success", int code = ApiCodes.Common.Success)
        => new(msg, code, null);

    public static ApiResponse<T> Failure<T>(string msg, int code, T? data = default)
        => new(msg, code, data);

    public static ApiResponse<object?> Failure(string msg, int code, object? data = null)
        => new(msg, code, data);
}
