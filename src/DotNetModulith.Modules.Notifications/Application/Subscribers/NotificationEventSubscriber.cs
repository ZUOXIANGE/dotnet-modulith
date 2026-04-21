using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Abstractions.Contracts.Payments;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Notifications.Application.Subscribers;

/// <summary>
/// 通知事件订阅者，监听订单和支付事件以发送通知
/// </summary>
public sealed class NotificationEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Notifications");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Notifications", "1.0.0");
    private static readonly Counter<long> NotificationsSent = Meter.CreateCounter<long>(
        "modulith.notifications.sent",
        unit: "{notification}",
        description: "Number of notifications sent");

    private readonly ILogger<NotificationEventSubscriber> _logger;

    public NotificationEventSubscriber(ILogger<NotificationEventSubscriber> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理订单创建事件，发送订单确认通知
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCreatedIntegrationEvent", Group = "notifications")]
    public Task HandleOrderCreatedAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SendOrderCreatedNotification", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCreatedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation(
            "OrderEntity confirmation: OrderEntity {OrderId} created for customer {CustomerId}, total {Amount}",
            @event.OrderId, @event.CustomerId, @event.TotalAmount);

        NotificationsSent.Add(1,
            new KeyValuePair<string, object?>("modulith.notification_type", "order_created"),
            new KeyValuePair<string, object?>("modulith.channel", "email"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理支付完成事件，发送支付回执通知
    /// </summary>
    [CapSubscribe("modulith.payments.PaymentCompletedIntegrationEvent", Group = "notifications")]
    public Task HandlePaymentCompletedAsync(PaymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SendPaymentCompletedNotification", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "PaymentCompletedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation(
            "PaymentEntity receipt: PaymentEntity {PaymentId} of {Amount} completed for order {OrderId}",
            @event.PaymentId, @event.Amount, @event.OrderId);

        NotificationsSent.Add(1,
            new KeyValuePair<string, object?>("modulith.notification_type", "payment_completed"),
            new KeyValuePair<string, object?>("modulith.channel", "email"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理订单取消事件，发送订单取消通知
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCancelledIntegrationEvent", Group = "notifications")]
    public Task HandleOrderCancelledAsync(OrderCancelledIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SendOrderCancelledNotification", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCancelledIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation(
            "OrderEntity cancellation: OrderEntity {OrderId} cancelled for customer {CustomerId}. Reason: {Reason}",
            @event.OrderId, @event.CustomerId, @event.Reason);

        NotificationsSent.Add(1,
            new KeyValuePair<string, object?>("modulith.notification_type", "order_cancelled"),
            new KeyValuePair<string, object?>("modulith.channel", "email"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理低库存预警事件，发送库存告警通知
    /// </summary>
    [CapSubscribe("modulith.inventory.LowStockDetectedIntegrationEvent", Group = "notifications")]
    public Task HandleLowStockDetectedAsync(LowStockDetectedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SendLowStockAlertNotification", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "LowStockDetectedIntegrationEvent");
        activity?.SetTag("modulith.threshold", @event.Threshold);
        activity?.SetTag("modulith.item_count", @event.Items.Count);

        _logger.LogWarning(
            "Low stock alert notification: {ItemCount} products are below threshold {Threshold}. Products: {Products}",
            @event.Items.Count,
            @event.Threshold,
            string.Join(", ", @event.Items.Select(item => $"{item.ProductId}:{item.AvailableQuantity}")));

        NotificationsSent.Add(
            @event.Items.Count,
            new KeyValuePair<string, object?>("modulith.notification_type", "low_stock_alert"),
            new KeyValuePair<string, object?>("modulith.channel", "message"));

        return Task.CompletedTask;
    }
}
