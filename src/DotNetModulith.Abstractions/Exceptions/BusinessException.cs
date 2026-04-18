namespace DotNetModulith.Abstractions.Exceptions;

using DotNetModulith.Abstractions.Results;

/// <summary>
/// 业务异常，可在应用层/领域层直接抛出并由全局异常处理统一转换为 API 响应
/// </summary>
public sealed class BusinessException : Exception
{
    /// <summary>
    /// 业务码（响应体中的 code 字段）
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int HttpStatusCode { get; }

    /// <summary>
    /// 附加业务数据（响应体中的 data 字段）
    /// </summary>
    public object? Payload { get; }

    public BusinessException(
        string message,
        int code = ApiCodes.Common.ValidationFailed,
        int httpStatusCode = 400,
        object? payload = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        HttpStatusCode = httpStatusCode;
        Payload = payload;
    }
}
