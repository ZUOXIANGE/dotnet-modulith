using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.ModulithCore.MultiTenancy;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using DotNetModulith.Modules.Orders.Infrastructure;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Subscribers;

/// <summary>
/// 支付事件订阅者，监听支付完成和支付失败事件以更新订单状态
/// </summary>
public sealed class PaymentEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly ILogger<PaymentEventSubscriber> _logger;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IFusionCache _cache;
    private readonly DbContextOptions<OrdersDbContext> _dbContextOptions;
    private readonly IMultiTenantStore<ModulithTenantInfo> _tenantStore;
    private readonly ICapPublisher _capPublisher;

    public PaymentEventSubscriber(
        ILogger<PaymentEventSubscriber> logger,
        IDomainEventDispatcher domainEventDispatcher,
        IFusionCache cache,
        DbContextOptions<OrdersDbContext> dbContextOptions,
        IMultiTenantStore<ModulithTenantInfo> tenantStore,
        ICapPublisher capPublisher)
    {
        _logger = logger;
        _domainEventDispatcher = domainEventDispatcher;
        _cache = cache;
        _dbContextOptions = dbContextOptions;
        _tenantStore = tenantStore;
        _capPublisher = capPublisher;
    }

    /// <summary>
    /// 处理支付完成事件，将订单标记为已支付
    /// </summary>
    [CapSubscribe("modulith.payments.PaymentCompletedIntegrationEvent", Group = "orders")]
    public async Task HandlePaymentCompletedAsync(PaymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandlePaymentCompleted", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "PaymentCompletedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        await using var tenantDbContext = await CreateTenantDbContextAsync(@event.TenantIdentifier);
        var orderRepository = new OrderRepository(tenantDbContext);
        var orderId = Guid.Parse(@event.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, ct);
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

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var domainEvent = new OrderPaidDomainEvent(
            order.Id,
            @event.TenantIdentifier,
            order.CustomerId,
            order.TotalAmount);

        await CapTransactionScope.ExecuteAsync(
            tenantDbContext,
            _capPublisher,
            async cancellationToken =>
            {
                await orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(
                    OrderCacheKeys.OrderDetail(@event.TenantIdentifier, order.Id.ToString()),
                    null,
                    cancellationToken);
                await _domainEventDispatcher.DispatchAsync([domainEvent], cancellationToken);
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

        await using var tenantDbContext = await CreateTenantDbContextAsync(@event.TenantIdentifier);
        var orderRepository = new OrderRepository(tenantDbContext);
        var orderId = Guid.Parse(@event.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, ct);
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

        var reason = $"Payment failed: {@event.Reason}";
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var lines = order.Lines
            .Select(line => new OrderLineData(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice))
            .ToList();

        var domainEvent = new OrderCancelledDomainEvent(
            order.Id,
            @event.TenantIdentifier,
            order.CustomerId,
            reason,
            lines);
        await CapTransactionScope.ExecuteAsync(
            tenantDbContext,
            _capPublisher,
            async cancellationToken =>
            {
                await orderRepository.UpdateAsync(order, cancellationToken);
                await _cache.RemoveAsync(
                    OrderCacheKeys.OrderDetail(@event.TenantIdentifier, order.Id.ToString()),
                    null,
                    cancellationToken);
                await _domainEventDispatcher.DispatchAsync([domainEvent], cancellationToken);
            },
            ct);

        _logger.LogWarning("Payment failed for order {OrderId}, order cancelled", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private async Task<OrdersDbContext> CreateTenantDbContextAsync(string tenantIdentifier)
    {
        var tenantInfo = await _tenantStore.GetByIdentifierAsync(tenantIdentifier)
            ?? throw new InvalidOperationException($"Tenant '{tenantIdentifier}' not found.");

        return new OrdersDbContext(_dbContextOptions, tenantInfo);
    }
}
