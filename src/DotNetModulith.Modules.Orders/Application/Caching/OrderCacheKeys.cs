namespace DotNetModulith.Modules.Orders.Application.Caching;

internal static class OrderCacheKeys
{
    public static string OrderDetail(string orderId) => $"orders:detail:{orderId}";
}
