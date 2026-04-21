using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Payments.Domain;
using DotNetModulith.Modules.Payments.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Payments.Application.Subscribers;

/// <summary>
/// 订单事件订阅者，监听订单创建事件以发起支付处理
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
    /// 处理库存预留成功事件，为订单创建支付记录并模拟支付处理
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
            _logger.LogInformation("PaymentEntity for order {OrderId} already exists, skip duplicate event", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                var payment = PaymentEntity.Create(@event.OrderId, @event.CustomerId, @event.TotalAmount);
                var success = SimulatePayment(@event.TotalAmount);

                if (success)
                {
                    payment.Complete($"TXN-{Guid.NewGuid():N}"[..20]);
                    await _paymentRepository.AddAsync(payment, cancellationToken);
                    await _domainEventDispatcher.DispatchAsync(payment, cancellationToken);

                    _logger.LogInformation("PaymentEntity completed for order {OrderId}", @event.OrderId);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return;
                }

                payment.Fail("PaymentEntity gateway timeout");
                await _paymentRepository.AddAsync(payment, cancellationToken);
                await _domainEventDispatcher.DispatchAsync(payment, cancellationToken);

                _logger.LogWarning("PaymentEntity failed for order {OrderId}", @event.OrderId);
                activity?.SetStatus(ActivityStatusCode.Error, "PaymentEntity failed");
            },
            ct);
    }

    /// <summary>
    /// 模拟支付网关处理（金额大于0即视为成功）
    /// </summary>
    /// <param name="amount">支付金额</param>
    /// <returns>支付是否成功</returns>
    private static bool SimulatePayment(decimal amount) => amount > 0;
}
