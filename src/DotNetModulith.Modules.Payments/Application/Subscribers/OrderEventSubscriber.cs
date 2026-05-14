using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Payments.Domain;
using DotNetModulith.Modules.Payments.Domain.Events;
using DotNetModulith.Modules.Payments.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Payments.Application.Subscribers;

/// <summary>
/// 订单事件订阅者，监听库存预留事件以处理支付
/// </summary>
public sealed class OrderEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Payments");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Payments", "1.0.0");
    private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
        "modulith.payments.events.consumed",
        unit: "{event}",
        description: "Number of events consumed by the Payments module");

    private readonly IPaymentRepository _paymentRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<OrderEventSubscriber> _logger;
    private readonly PaymentsDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public OrderEventSubscriber(
        IPaymentRepository paymentRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<OrderEventSubscriber> logger,
        PaymentsDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _paymentRepository = paymentRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    /// <summary>
    /// 处理库存预留完成事件，执行支付流程
    /// </summary>
    [CapSubscribe("modulith.inventory.StockReservedIntegrationEvent", Group = "payments")]
    public async Task HandleOrderCreatedAsync(StockReservedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleStockReserved_ProcessPayment", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "StockReservedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Processing payment for order {OrderId}, amount {Amount}",
            @event.OrderId, @event.TotalAmount);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "StockReservedIntegrationEvent"));

        var existingPayment = await _paymentRepository.GetByOrderIdAsync(@event.OrderId, ct);
        if (existingPayment is not null)
        {
            _logger.LogInformation("Payment for order {OrderId} already exists, skip duplicate event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                var paymentId = Guid.NewGuid();
                var payment = new PaymentEntity
                {
                    Id = paymentId,
                    OrderId = @event.OrderId,
                    CustomerId = @event.CustomerId,
                    Amount = @event.TotalAmount,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var success = SimulatePayment(@event.TotalAmount);

                if (success)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionId = $"TXN-{Guid.NewGuid():N}"[..20];
                    payment.CompletedAt = DateTimeOffset.UtcNow;

                    var domainEvent = new PaymentCompletedDomainEvent(
                        payment.Id, payment.OrderId, payment.CustomerId, payment.Amount);

                    await _paymentRepository.AddAsync(payment, cancellationToken);
                    await _domainEventDispatcher.DispatchAsync([domainEvent], cancellationToken);

                    _logger.LogInformation("Payment completed for order {OrderId}", @event.OrderId);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return;
                }

                payment.Status = PaymentStatus.Failed;
                payment.CompletedAt = DateTimeOffset.UtcNow;

                var failedDomainEvent = new PaymentFailedDomainEvent(
                    payment.Id, payment.OrderId, payment.CustomerId, "Payment gateway timeout");

                await _paymentRepository.AddAsync(payment, cancellationToken);
                await _domainEventDispatcher.DispatchAsync([failedDomainEvent], cancellationToken);

                _logger.LogWarning("Payment failed for order {OrderId}", @event.OrderId);
                activity?.SetStatus(ActivityStatusCode.Error, "Payment failed");
            },
            ct);
    }

    private static bool SimulatePayment(decimal amount) => amount > 0;
}
