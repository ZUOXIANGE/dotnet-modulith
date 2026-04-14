using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Events;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;

/// <summary>
/// 创建订单命令处理器
/// </summary>
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderId>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Orders", "1.0.0");
    private static readonly Histogram<double> OrderCreationDuration = Meter.CreateHistogram<double>(
        "modulith.orders.creation.duration",
        unit: "ms",
        description: "Duration of order creation");

    private readonly IOrderRepository _orderRepository;
    private readonly DomainEventToIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        DomainEventToIntegrationEventPublisher eventPublisher,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async ValueTask<OrderId> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("CreateOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.customer_id", command.CustomerId);
        activity?.SetTag("modulith.line_count", command.Lines.Count);

        try
        {
            var order = Order.Create(command.CustomerId, command.Lines);

            await _orderRepository.AddAsync(order, cancellationToken);
            await _eventPublisher.PublishAsync(order, cancellationToken);

            order.ClearDomainEvents();

            _logger.LogInformation("Order {OrderId} created for customer {CustomerId} with total {TotalAmount}",
                order.Id, order.CustomerId, order.TotalAmount);

            activity?.SetTag("modulith.order_id", order.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            return order.Id;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = ex.GetType().FullName,
                ["exception.message"] = ex.Message
            }));

            throw;
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startTime);
            OrderCreationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("modulith.outcome", "success"));
        }
    }
}
