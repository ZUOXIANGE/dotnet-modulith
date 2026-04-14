using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using Microsoft.Extensions.Logging;

using OrderContracts = DotNetModulith.Abstractions.Contracts.Orders;

namespace DotNetModulith.Modules.Orders.Application.Events;

/// <summary>
/// 领域事件到集成事件的发布器，将订单聚合根的领域事件转换为集成事件并通过CAP发布
/// </summary>
public sealed class DomainEventToIntegrationEventPublisher
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Orders", "1.0.0");
    private static readonly Counter<long> EventsPublished = Meter.CreateCounter<long>(
        "modulith.orders.events.published",
        unit: "{event}",
        description: "Number of integration events published by the Orders module");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<DomainEventToIntegrationEventPublisher> _logger;

    public DomainEventToIntegrationEventPublisher(
        ICapPublisher capPublisher,
        ILogger<DomainEventToIntegrationEventPublisher> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    /// <summary>
    /// 发布订单聚合根中的所有领域事件为集成事件
    /// </summary>
    /// <param name="order">包含待发布领域事件的订单聚合根</param>
    /// <param name="ct">取消令牌</param>
    public async Task PublishAsync(Order order, CancellationToken ct = default)
    {
        foreach (var domainEvent in order.DomainEvents)
        {
            using var activity = ActivitySource.StartActivity($"PublishIntegrationEvent.{domainEvent.GetType().Name}", ActivityKind.Producer);

            var integrationEvent = MapToIntegrationEvent(domainEvent, order);

            if (integrationEvent is null)
                continue;

            var topic = GetTopicName(integrationEvent);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", topic);
            activity?.SetTag("messaging.destination_kind", "topic");
            activity?.SetTag("modulith.event_type", integrationEvent.EventType);

            _logger.LogInformation("Publishing integration event {EventType} to topic {Topic} with EventId {EventId}",
                integrationEvent.EventType, topic, integrationEvent.EventId);

            await _capPublisher.PublishAsync(topic, integrationEvent, cancellationToken: ct);

            EventsPublished.Add(1, new KeyValuePair<string, object?>("modulith.event_type", integrationEvent.EventType));
        }
    }

    /// <summary>
    /// 将领域事件映射为对应的集成事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    /// <param name="order">订单聚合根</param>
    /// <returns>对应的集成事件，无法映射时返回null</returns>
    private static IIntegrationEvent? MapToIntegrationEvent(IDomainEvent domainEvent, Order order) =>
        domainEvent switch
        {
            OrderCreatedDomainEvent => new OrderContracts.OrderCreatedIntegrationEvent(
                order.Id.ToString(),
                order.CustomerId,
                order.TotalAmount,
                order.Lines.Select(l => new OrderContracts.OrderLineContract(
                    l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList()),

            OrderPaidDomainEvent => new OrderContracts.OrderPaidIntegrationEvent(
                order.Id.ToString(),
                order.CustomerId,
                order.TotalAmount),

            OrderCancelledDomainEvent cancelled => new OrderContracts.OrderCancelledIntegrationEvent(
                order.Id.ToString(),
                order.CustomerId,
                cancelled.Reason),

            _ => null
        };

    /// <summary>
    /// 根据集成事件类型生成消息主题名称
    /// </summary>
    /// <param name="integrationEvent">集成事件</param>
    /// <returns>消息主题名称</returns>
    private static string GetTopicName(IIntegrationEvent integrationEvent) =>
        $"modulith.orders.{integrationEvent.EventType}";
}
