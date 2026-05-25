using System.Diagnostics;
using DotNetModulith.ModulithCore.MultiTenancy;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Application.Mappings;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Queries.GetOrder;

/// <summary>
/// 获取订单查询处理器
/// </summary>
public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, OrderDetail?>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly IOrderRepository _orderRepository;
    private readonly IFusionCache _cache;
    private readonly IModulithTenantAccessor _tenantAccessor;

    public GetOrderQueryHandler(
        IOrderRepository orderRepository,
        IFusionCache cache,
        IModulithTenantAccessor tenantAccessor)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _tenantAccessor = tenantAccessor;
    }

    public async ValueTask<OrderDetail?> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", query.OrderId.ToString());
        var cacheKey = OrderCacheKeys.OrderDetail(
            _tenantAccessor.GetRequiredTenantIdentifier(),
            query.OrderId.ToString());

        return await _cache.GetOrSetAsync<OrderDetail?>(
            cacheKey,
            async (_, ct) =>
            {
                var order = await _orderRepository.GetByIdAsync(query.OrderId, ct);
                return order?.ToDetail();
            },
            default,
            null,
            null,
            cancellationToken);
    }
}
