using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Inventory.Application.Subscribers;

/// <summary>
/// 订单事件订阅者，监听订单创建事件以预留库存
/// </summary>
public sealed class OrderEventSubscriber : ICapSubscribe
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Inventory", "1.0.0");
    private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
        "modulith.inventory.events.consumed",
        unit: "{event}",
        description: "Number of events consumed by the Inventory module");

    private readonly IStockRepository _stockRepository;
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OrderEventSubscriber> _logger;

    public OrderEventSubscriber(
        IStockRepository stockRepository,
        ICapPublisher capPublisher,
        ILogger<OrderEventSubscriber> logger)
    {
        _stockRepository = stockRepository;
        _capPublisher = capPublisher;
        _logger = logger;
    }

    /// <summary>
    /// 处理订单创建事件，为订单中的产品预留库存
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCreatedIntegrationEvent")]
    public async Task HandleOrderCreatedAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleOrderCreated", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCreatedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Processing order {OrderId} for inventory reservation", @event.OrderId);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "OrderCreatedIntegrationEvent"));

        foreach (var line in @event.Lines)
        {
            var stock = await _stockRepository.GetByProductIdAsync(line.ProductId, ct);

            if (stock is null || !stock.TryReserve(line.Quantity))
            {
                _logger.LogWarning("Insufficient stock for product {ProductId} in order {OrderId}",
                    line.ProductId, @event.OrderId);

                var insufficientEvent = new StockInsufficientIntegrationEvent(
                    @event.OrderId, line.ProductId, line.Quantity, stock?.AvailableQuantity ?? 0);

                await _capPublisher.PublishAsync(
                    "modulith.inventory.StockInsufficientIntegrationEvent",
                    insufficientEvent, cancellationToken: ct);

                activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
                return;
            }

            await _stockRepository.UpdateAsync(stock, ct);
        }

        var reservedEvent = new StockReservedIntegrationEvent(@event.OrderId, @event.Lines.First().ProductId, @event.Lines.Sum(l => l.Quantity));
        await _capPublisher.PublishAsync(
            "modulith.inventory.StockReservedIntegrationEvent",
            reservedEvent, cancellationToken: ct);

        _logger.LogInformation("Stock reserved for order {OrderId}", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
