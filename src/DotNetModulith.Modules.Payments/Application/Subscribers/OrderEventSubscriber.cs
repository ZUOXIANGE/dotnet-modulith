using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Modules.Payments.Domain;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Payments.Application.Subscribers;

/// <summary>
/// 订单事件订阅者，监听订单创建事件以发起支付处理
/// </summary>
internal sealed class OrderEventSubscriber
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Payments");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Payments", "1.0.0");
    private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
        "modulith.payments.events.consumed",
        unit: "{event}",
        description: "Number of events consumed by the Payments module");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OrderEventSubscriber> _logger;

    public OrderEventSubscriber(ICapPublisher capPublisher, ILogger<OrderEventSubscriber> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    /// <summary>
    /// 处理订单创建事件，为订单创建支付记录并模拟支付处理
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCreatedIntegrationEvent")]
    public async Task HandleOrderCreatedAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleOrderCreated_ProcessPayment", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCreatedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Processing payment for order {OrderId}, amount {Amount}",
            @event.OrderId, @event.TotalAmount);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "OrderCreatedIntegrationEvent"));

        var payment = Payment.Create(@event.OrderId, @event.CustomerId, @event.TotalAmount);

        var success = SimulatePayment(@event.TotalAmount);

        if (success)
        {
            payment.Complete($"TXN-{Guid.NewGuid():N}"[..20]);

            var completedEvent = new PaymentCompletedIntegrationEvent(
                @event.OrderId, payment.Id.ToString(), @event.TotalAmount);

            await _capPublisher.PublishAsync(
                "modulith.payments.PaymentCompletedIntegrationEvent",
                completedEvent, cancellationToken: ct);

            _logger.LogInformation("Payment completed for order {OrderId}", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            payment.Fail("Payment gateway timeout");

            var failedEvent = new PaymentFailedIntegrationEvent(
                @event.OrderId, payment.Id.ToString(), "Payment gateway timeout");

            await _capPublisher.PublishAsync(
                "modulith.payments.PaymentFailedIntegrationEvent",
                failedEvent, cancellationToken: ct);

            _logger.LogWarning("Payment failed for order {OrderId}", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, "Payment failed");
        }
    }

    /// <summary>
    /// 模拟支付网关处理（金额大于0即视为成功）
    /// </summary>
    /// <param name="amount">支付金额</param>
    /// <returns>支付是否成功</returns>
    private static bool SimulatePayment(decimal amount) => amount > 0;
}
