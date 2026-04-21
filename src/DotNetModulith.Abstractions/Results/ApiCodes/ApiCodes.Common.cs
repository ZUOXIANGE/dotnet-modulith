namespace DotNetModulith.Abstractions.Results;

public static partial class ApiCodes
{
    /// <summary>
    /// 通用业务码
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// 成功
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// 参数校验失败
        /// </summary>
        public const int ValidationFailed = 400;

        /// <summary>
        /// 未认证
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// 无权限
        /// </summary>
        public const int Forbidden = 403;

        /// <summary>
        /// 资源不存在
        /// </summary>
        public const int NotFound = 404;

        /// <summary>
        /// 服务器内部错误
        /// </summary>
        public const int InternalError = 500;
    }
}
