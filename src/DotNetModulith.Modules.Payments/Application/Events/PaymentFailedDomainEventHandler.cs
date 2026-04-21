using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Payments.Application.Mappings;
using DotNetModulith.Modules.Payments.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Payments.Application.Events;

/// <summary>
/// 支付失败领域事件处理器，将领域事件转换为集成事件并通过CAP发布
/// </summary>
public sealed class PaymentFailedDomainEventHandler : IDomainEventHandler<PaymentFailedDomainEvent>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Payments");

    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<PaymentFailedDomainEventHandler> _logger;

    public PaymentFailedDomainEventHandler(
        ICapPublisher capPublisher,
        ILogger<PaymentFailedDomainEventHandler> logger)
    {
        _capPublisher = capPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentFailedDomainEvent domainEvent, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("PublishIntegrationEvent.PaymentFailedIntegrationEvent", ActivityKind.Producer);

        var integrationEvent = domainEvent.ToIntegrationEvent();
        var topic = $"modulith.payments.{integrationEvent.EventType}";

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("modulith.event_type", integrationEvent.EventType);

        _logger.LogInformation("Publishing integration event {EventType} to topic {Topic} with EventId {EventId}",
            integrationEvent.EventType, topic, integrationEvent.EventId);

        await _capPublisher.PublishAsync(topic, integrationEvent, cancellationToken: ct);
    }
}
