using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Mappings;
using DotNetModulith.Modules.Orders.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Application.Events;

/// <summary>
/// 订单创建领域事件处理器，将领域事件转换为集成事件并通过CAP发布
/// </summary>
public sealed class OrderCreatedDomainEventHandler : IDomainEventHandler<OrderCreatedDomainEvent>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Orders", "1.0.0");
    private static readonly Counter<long> EventsPublished = Meter.CreateCounter<long>(
        "modulith.orders.events.published",
        unit: "{event}",
        description: "Number of integration events published by the Orders module");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OrderCreatedDomainEventHandler> _logger;

    public OrderCreatedDomainEventHandler(
        ICapPublisher capPublisher,
        ILogger<OrderCreatedDomainEventHandler> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedDomainEvent domainEvent, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("PublishIntegrationEvent.OrderCreatedIntegrationEvent", ActivityKind.Producer);

        var integrationEvent = domainEvent.ToIntegrationEvent();
        var topic = $"modulith.orders.{integrationEvent.EventType}";

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
/// 订单支付领域事件处理器，将领域事件转换为集成事件并通过CAP发布
/// </summary>
public sealed class OrderPaidDomainEventHandler : IDomainEventHandler<OrderPaidDomainEvent>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OrderPaidDomainEventHandler> _logger;

    public OrderPaidDomainEventHandler(
        ICapPublisher capPublisher,
        ILogger<OrderPaidDomainEventHandler> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPaidDomainEvent domainEvent, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("PublishIntegrationEvent.OrderPaidIntegrationEvent", ActivityKind.Producer);

        var integrationEvent = domainEvent.ToIntegrationEvent();
        var topic = $"modulith.orders.{integrationEvent.EventType}";

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("modulith.event_type", integrationEvent.EventType);

        _logger.LogInformation("Publishing integration event {EventType} to topic {Topic} with EventId {EventId}",
            integrationEvent.EventType, topic, integrationEvent.EventId);

        await _capPublisher.PublishAsync(topic, integrationEvent, cancellationToken: ct);
    }
}

/// <summary>
/// 订单取消领域事件处理器，将领域事件转换为集成事件并通过CAP发布
/// </summary>
public sealed class OrderCancelledDomainEventHandler : IDomainEventHandler<OrderCancelledDomainEvent>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OrderCancelledDomainEventHandler> _logger;

    public OrderCancelledDomainEventHandler(
        ICapPublisher capPublisher,
        ILogger<OrderCancelledDomainEventHandler> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCancelledDomainEvent domainEvent, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("PublishIntegrationEvent.OrderCancelledIntegrationEvent", ActivityKind.Producer);

        var integrationEvent = domainEvent.ToIntegrationEvent();
        var topic = $"modulith.orders.{integrationEvent.EventType}";

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("modulith.event_type", integrationEvent.EventType);

        _logger.LogInformation("Publishing integration event {EventType} to topic {Topic} with EventId {EventId}",
            integrationEvent.EventType, topic, integrationEvent.EventId);

        await _capPublisher.PublishAsync(topic, integrationEvent, cancellationToken: ct);
    }
}
