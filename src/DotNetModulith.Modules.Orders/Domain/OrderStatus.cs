namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// 待确认
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已确认
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// 已支付
    /// </summary>
    Paid = 2,

    /// <summary>
    /// 已发货
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}
