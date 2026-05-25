namespace DotNetModulith.Modules.Orders.Application.Caching;

/// <summary>
/// 订单缓存键生成器
/// </summary>
internal static class OrderCacheKeys
{
    /// <summary>
    /// 生成订单详情缓存键
    /// </summary>
    /// <param name="tenantIdentifier">租户标识</param>
    /// <param name="orderId">订单ID</param>
    /// <returns>缓存键字符串</returns>
    public static string OrderDetail(string tenantIdentifier, string orderId)
        => $"orders:{tenantIdentifier}:detail:{orderId}";
}
