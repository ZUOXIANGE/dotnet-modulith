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

    /// <summary>
    /// 认证域业务码
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        public const int InvalidCredentials = 40001;

        /// <summary>
        /// 令牌无效或已失效
        /// </summary>
        public const int InvalidToken = 40002;

        /// <summary>
        /// 用户已被禁用
        /// </summary>
        public const int UserDisabled = 40003;
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

    /// <summary>
    /// 用户与角色域业务码
    /// </summary>
    public static class User
    {
        /// <summary>
        /// 用户名已存在
        /// </summary>
        public const int UserNameAlreadyExists = 50001;

        /// <summary>
        /// 邮箱已存在
        /// </summary>
        public const int EmailAlreadyExists = 50002;

        /// <summary>
        /// 角色不存在
        /// </summary>
        public const int RoleNotFound = 50003;

        /// <summary>
        /// 权限编码不合法
        /// </summary>
        public const int InvalidPermission = 50004;

        /// <summary>
        /// 角色名已存在
        /// </summary>
        public const int RoleNameAlreadyExists = 50005;

        /// <summary>
        /// 当前密码不正确
        /// </summary>
        public const int InvalidCurrentPassword = 50006;
    }
}
