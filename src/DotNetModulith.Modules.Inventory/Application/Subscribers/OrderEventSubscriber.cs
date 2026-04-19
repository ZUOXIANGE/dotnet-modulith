using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
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
    private readonly InventoryDbContext _dbContext;

    public OrderEventSubscriber(
        IStockRepository stockRepository,
        ICapPublisher capPublisher,
        ILogger<OrderEventSubscriber> logger,
        InventoryDbContext dbContext)
    {
        _stockRepository = stockRepository;
        _capPublisher = capPublisher;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理订单创建事件，为订单中的产品预留库存
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCreatedIntegrationEvent", Group = "inventory")]
    public async Task HandleOrderCreatedAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleOrderCreated", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCreatedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Processing order {OrderId} for inventory reservation", @event.OrderId);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "OrderCreatedIntegrationEvent"));

        var existingReservations = await _stockRepository.GetReservationsByOrderIdAsync(@event.OrderId, ct);
        if (existingReservations.Count > 0)
        {
            _logger.LogInformation("Inventory reservations for order {OrderId} already exist, skip duplicate order created event",
                @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        var reservedLines = new List<StockReservedLine>();

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                foreach (var line in @event.Lines)
                {
                    var stock = await _stockRepository.GetByProductIdAsync(line.ProductId, cancellationToken);

                    if (stock is null || !stock.TryReserve(line.Quantity))
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId} in order {OrderId}",
                            line.ProductId, @event.OrderId);

                        foreach (var reserved in reservedLines)
                        {
                            var reservedStock = await _stockRepository.GetByProductIdAsync(reserved.ProductId, cancellationToken);
                            if (reservedStock is not null)
                            {
                                reservedStock.Release(reserved.Quantity);
                                await _stockRepository.UpdateAsync(reservedStock, cancellationToken);
                            }
                        }

                        var insufficientEvent = new StockInsufficientIntegrationEvent(
                            @event.OrderId, line.ProductId, line.Quantity, stock?.AvailableQuantity ?? 0);

                        await _capPublisher.PublishAsync(
                            "modulith.inventory.StockInsufficientIntegrationEvent",
                            insufficientEvent,
                            cancellationToken: cancellationToken);

                        activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
                        return;
                    }

                    await _stockRepository.UpdateAsync(stock, cancellationToken);
                    await _stockRepository.AddReservationAsync(
                        StockReservation.Create(stock.Id, @event.OrderId, line.ProductId, line.Quantity),
                        cancellationToken);
                    reservedLines.Add(new StockReservedLine(line.ProductId, line.Quantity));
                }

                var reservedEvent = new StockReservedIntegrationEvent(
                    @event.OrderId,
                    @event.CustomerId,
                    @event.TotalAmount,
                    reservedLines);

                await _capPublisher.PublishAsync(
                    "modulith.inventory.StockReservedIntegrationEvent",
                    reservedEvent,
                    cancellationToken: cancellationToken);
            },
            ct);

        _logger.LogInformation("Stock reserved for order {OrderId}", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// 处理订单取消事件，释放此前已预留的库存
    /// </summary>
    [CapSubscribe("modulith.orders.OrderCancelledIntegrationEvent", Group = "inventory")]
    public async Task HandleOrderCancelledAsync(OrderCancelledIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleOrderCancelled", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderCancelledIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        var reservations = await _stockRepository.GetReservationsByOrderIdAsync(@event.OrderId, ct);
        var pendingReservations = reservations
            .Where(r => r.Status == StockReservationStatus.Pending)
            .ToList();

        if (pendingReservations.Count == 0)
        {
            _logger.LogInformation("No pending stock reservations found for cancelled order {OrderId}", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                foreach (var reservation in pendingReservations)
                {
                    var stock = await _stockRepository.GetByIdAsync(reservation.StockId, cancellationToken);
                    if (stock is null || stock.ReservedQuantity < reservation.Quantity)
                    {
                        continue;
                    }

                    stock.Release(reservation.Quantity);
                    reservation.Release();
                    await _stockRepository.UpdateAsync(stock, cancellationToken);
                    await _stockRepository.UpdateReservationAsync(reservation, cancellationToken);
                }
            },
            ct);

        _logger.LogInformation("Released reserved stock for cancelled order {OrderId}", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// 处理订单支付完成事件，确认库存预留并结束占用
    /// </summary>
    [CapSubscribe("modulith.orders.OrderPaidIntegrationEvent", Group = "inventory")]
    public async Task HandleOrderPaidAsync(OrderPaidIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleOrderPaid", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "OrderPaidIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        var reservations = await _stockRepository.GetReservationsByOrderIdAsync(@event.OrderId, ct);
        var pendingReservations = reservations
            .Where(r => r.Status == StockReservationStatus.Pending)
            .ToList();

        if (pendingReservations.Count == 0)
        {
            _logger.LogInformation("No pending stock reservations found for paid order {OrderId}", @event.OrderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async cancellationToken =>
            {
                foreach (var reservation in pendingReservations)
                {
                    var stock = await _stockRepository.GetByIdAsync(reservation.StockId, cancellationToken);
                    if (stock is null || stock.ReservedQuantity < reservation.Quantity)
                    {
                        continue;
                    }

                    stock.ConfirmReservation(reservation.Quantity);
                    reservation.Confirm();
                    await _stockRepository.UpdateAsync(stock, cancellationToken);
                    await _stockRepository.UpdateReservationAsync(reservation, cancellationToken);
                }
            },
            ct);

        _logger.LogInformation("Confirmed stock reservations for paid order {OrderId}", @event.OrderId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
