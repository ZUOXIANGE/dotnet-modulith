using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using Mediator;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

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
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly IFusionCache _cache;
    private readonly OrdersDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<CreateOrderCommandHandler> logger,
        IFusionCache cache,
        OrdersDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _orderRepository = orderRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
        _cache = cache;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    public async ValueTask<OrderId> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("CreateOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.customer_id", command.CustomerId);
        activity?.SetTag("modulith.line_count", command.Lines.Count);

        try
        {
            var order = OrderEntity.Create(command.CustomerId, command.Lines);

            await CapTransactionScope.ExecuteAsync(
                _dbContext,
                _capPublisher,
                async ct =>
                {
                    await _orderRepository.AddAsync(order, ct);
                    await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, ct);
                    await _domainEventDispatcher.DispatchAsync(order, ct);
                },
                cancellationToken);

            _logger.LogInformation("OrderEntity {OrderId} created for customer {CustomerId} with total {TotalAmount}",
                order.Id, order.CustomerId, order.TotalAmount);

            activity?.SetTag("modulith.order_id", order.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            return order.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}",
                command.CustomerId);

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
                new KeyValuePair<string, object?>("modulith.outcome", Activity.Current?.Status == ActivityStatusCode.Error ? "failure" : "success"));
        }
    }
}
