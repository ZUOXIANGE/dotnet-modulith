namespace DotNetModulith.Modules.Payments.Domain;

/// <summary>
/// 支付状态枚举
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 已失败
    /// </summary>
    Failed = 2,

    /// <summary>
    /// 已退款
    /// </summary>
    Refunded = 3
}
