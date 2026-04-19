using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Subscribers;

/// <summary>
/// 库存事件订阅者，监听库存模块发布的集成事件
/// </summary>
public sealed class InventoryEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Orders", "1.0.0");
    private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
        "modulith.orders.events.consumed",
        unit: "{event}",
        description: "Number of events consumed by the Orders module");

    private readonly ILogger<InventoryEventSubscriber> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IFusionCache _cache;
    private readonly OrdersDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public InventoryEventSubscriber(
        ILogger<InventoryEventSubscriber> logger,
        IOrderRepository orderRepository,
        IDomainEventDispatcher domainEventDispatcher,
        IFusionCache cache,
        OrdersDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _cache = cache;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    /// <summary>
    /// 处理库存已预留事件，确认订单
    /// </summary>
    [CapSubscribe("modulith.inventory.StockReservedIntegrationEvent", Group = "orders")]
    public async Task HandleStockReservedAsync(StockReservedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleStockReserved", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "StockReservedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "StockReservedIntegrationEvent"));

        var orderId = new OrderId(Guid.Parse(@event.OrderId));
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found while handling reserved stock event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            return;
        }

        if (order.Status is OrderStatus.Confirmed or OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already advanced to {Status}, skip duplicate stock reserved event",
                order.Id, order.Status);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Order {OrderId} is already cancelled, skip stock reserved event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                order.Confirm();
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, cancellationToken);
                await _domainEventDispatcher.DispatchAsync(order, cancellationToken);
            },
            ct);

        _logger.LogInformation(
            "Stock reserved for order {OrderId} with {LineCount} lines, order confirmed",
            @event.OrderId, @event.Lines.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// 处理库存不足事件，取消订单
    /// </summary>
    [CapSubscribe("modulith.inventory.StockInsufficientIntegrationEvent", Group = "orders")]
    public async Task HandleStockInsufficientAsync(StockInsufficientIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleStockInsufficient", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "StockInsufficientIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);
        activity?.SetTag("modulith.product_id", @event.ProductId);

        var orderId = new OrderId(Guid.Parse(@event.OrderId));
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found while handling insufficient stock event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} already cancelled, skip duplicate insufficient stock event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogError("Received insufficient stock for paid order {OrderId}", order.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Order already paid");
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                order.Cancel($"Insufficient stock for product {@event.ProductId}.");
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, cancellationToken);
                await _domainEventDispatcher.DispatchAsync(order, cancellationToken);
            },
            ct);

        _logger.LogWarning("Order {OrderId} cancelled due to insufficient stock for product {ProductId}",
            order.Id, @event.ProductId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
