using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Subscribers;

/// <summary>
/// 支付事件订阅者，监听支付模块发布的集成事件
/// </summary>
public sealed class PaymentEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly ILogger<PaymentEventSubscriber> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IFusionCache _cache;
    private readonly OrdersDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public PaymentEventSubscriber(
        ILogger<PaymentEventSubscriber> logger,
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
    /// 处理支付完成事件，更新订单状态为已支付
    /// </summary>
    [CapSubscribe("modulith.payments.PaymentCompletedIntegrationEvent", Group = "orders")]
    public async Task HandlePaymentCompletedAsync(PaymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandlePaymentCompleted", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "PaymentCompletedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        var orderId = new OrderId(Guid.Parse(@event.OrderId));
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found while handling payment completed event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            return;
        }

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already paid, skip duplicate payment completed event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Order {OrderId} is cancelled, ignore payment completed event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        if (order.Status == OrderStatus.Pending)
        {
            _logger.LogWarning("Order {OrderId} is still pending, ignore out-of-order payment completed event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                order.MarkAsPaid();
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, cancellationToken);
                await _domainEventDispatcher.DispatchAsync(order, cancellationToken);
            },
            ct);

        _logger.LogInformation("Payment completed for order {OrderId}, amount {Amount}",
            @event.OrderId, @event.Amount);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// 处理支付失败事件，取消订单
    /// </summary>
    [CapSubscribe("modulith.payments.PaymentFailedIntegrationEvent", Group = "orders")]
    public async Task HandlePaymentFailedAsync(PaymentFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandlePaymentFailed", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "PaymentFailedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        var orderId = new OrderId(Guid.Parse(@event.OrderId));
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found while handling payment failed event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} already cancelled, skip duplicate payment failed event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogWarning("Order {OrderId} is already paid, ignore payment failed event", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                order.Cancel($"Payment failed: {@event.Reason}");
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, cancellationToken);
                await _domainEventDispatcher.DispatchAsync(order, cancellationToken);
            },
            ct);

        _logger.LogWarning("Payment failed for order {OrderId}, order cancelled", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
