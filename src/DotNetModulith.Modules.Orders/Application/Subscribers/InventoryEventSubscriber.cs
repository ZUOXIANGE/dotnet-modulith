using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Modules.Orders.Domain;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Application.Subscribers;

/// <summary>
/// 库存事件订阅者，监听库存模块发布的集成事件
/// </summary>
internal sealed class InventoryEventSubscriber
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Orders", "1.0.0");
    private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
        "modulith.orders.events.consumed",
        unit: "{event}",
        description: "Number of events consumed by the Orders module");

    private readonly ILogger<InventoryEventSubscriber> _logger;

    public InventoryEventSubscriber(ILogger<InventoryEventSubscriber> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理库存已预留事件，确认订单
    /// </summary>
    [CapSubscribe("modulith.inventory.StockReservedIntegrationEvent")]
    public async Task HandleStockReservedAsync(StockReservedIntegrationEvent @event, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HandleStockReserved", ActivityKind.Consumer);
        activity?.SetTag("modulith.event_type", "StockReservedIntegrationEvent");
        activity?.SetTag("modulith.order_id", @event.OrderId);

        _logger.LogInformation("Stock reserved for order {OrderId}, proceeding with confirmation", @event.OrderId);

        EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "StockReservedIntegrationEvent"));

        await Task.CompletedTask;
    }
}
