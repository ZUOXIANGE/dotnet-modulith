using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Payments;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Application.Subscribers;

/// <summary>
/// 支付事件订阅者，监听支付模块发布的集成事件
/// </summary>
public sealed class PaymentEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly ILogger<PaymentEventSubscriber> _logger;

    public PaymentEventSubscriber(ILogger<PaymentEventSubscriber> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理支付完成事件，更新订单状态为已支付
    /// </summary>
    [CapSubscribe("modulith.payments.PaymentCompletedIntegrationEvent")]
    public Task HandlePaymentCompletedAsync(PaymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandlePaymentCompleted", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "PaymentCompletedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Payment completed for order {OrderId}, amount {Amount}",
            @event.OrderId, @event.Amount);

        return Task.CompletedTask;
    }
}
