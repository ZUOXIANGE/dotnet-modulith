namespace DotNetModulith.Abstractions.Results;

/// <summary>
/// 统一 API 业务码定义
/// </summary>
public static class ApiCodes
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
        /// 资源不存在
        /// </summary>
        public const int NotFound = 404;

        /// <summary>
        /// 服务器内部错误
        /// </summary>
        public const int InternalError = 500;
    }

    /// <summary>
    /// 订单域业务码
    /// </summary>
    public static class Order
    {
        /// <summary>
        /// 订单状态不允许当前操作
        /// </summary>
        public const int InvalidState = 10001;
    }

    /// <summary>
    /// 库存域业务码
    /// </summary>
    public static class Inventory
    {
        /// <summary>
        /// 库存不足
        /// </summary>
        public const int InsufficientStock = 20001;
    }

    /// <summary>
    /// 支付域业务码
    /// </summary>
    public static class Payment
    {
        /// <summary>
        /// 支付处理失败
        /// </summary>
        public const int ProcessingFailed = 30001;
    }
}
