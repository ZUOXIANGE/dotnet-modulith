using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Api;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using DotNetModulith.Modules.Orders.Infrastructure;
using Mediator;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;

/// <summary>
/// 创建订单命令处理器
/// </summary>
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
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
    private readonly IInventoryService _inventoryService;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<CreateOrderCommandHandler> logger,
        IFusionCache cache,
        OrdersDbContext dbContext,
        ICapPublisher capPublisher,
        IInventoryService inventoryService)
    {
        _orderRepository = orderRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
        _cache = cache;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
        _inventoryService = inventoryService;
    }

    public async ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("CreateOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.customer_id", command.CustomerId);
        activity?.SetTag("modulith.line_count", command.Lines.Count);

        try
        {
            if (string.IsNullOrWhiteSpace(command.CustomerId))
                throw new ArgumentException("Customer ID is required.", nameof(command.CustomerId));

            if (command.Lines.Count == 0)
                throw new ArgumentException("Order must have at least one line.", nameof(command.Lines));

            var checkStockLines = command.Lines
                .Select(line => new CheckStockLine(line.ProductId, line.Quantity))
                .ToList();

            var checkResult = await _inventoryService.CheckStockAsync(checkStockLines, cancellationToken);

            if (!checkResult.IsSuccess)
            {
                throw new BusinessException(
                    message: checkResult.Error!,
                    code: ApiCodes.Inventory.InsufficientStock,
                    httpStatusCode: 422);
            }

            var orderId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var orderLines = command.Lines
                .Select(line => new OrderLineEntity(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice))
                .ToList();

            var totalAmount = orderLines.Sum(l => l.Quantity * l.UnitPrice);

            var order = new OrderEntity
            {
                Id = orderId,
                CustomerId = command.CustomerId,
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount,
                CreatedAt = now,
                Lines = orderLines
            };

            var domainEvent = new OrderCreatedDomainEvent(order.Id, order.CustomerId, order.TotalAmount, command.Lines);

            await CapTransactionScope.ExecuteAsync(
                _dbContext,
                _capPublisher,
                async ct =>
                {
                    await _orderRepository.AddAsync(order, ct);
                    await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, ct);
                    await _domainEventDispatcher.DispatchAsync([domainEvent], ct);
                },
                cancellationToken);

            var reserveLines = command.Lines
                .Select(line => new ReserveStockLine(line.ProductId, line.Quantity))
                .ToList();

            var reserveResult = await _inventoryService.ReserveStockAsync(
                order.Id.ToString(), order.CustomerId, order.TotalAmount, reserveLines, cancellationToken);

            if (!reserveResult.IsSuccess)
            {
                _logger.LogWarning("Stock reservation failed after order {OrderId} created: {Error}",
                    order.Id, reserveResult.Error);

                throw new BusinessException(
                    message: reserveResult.Error!,
                    code: ApiCodes.Inventory.InsufficientStock,
                    httpStatusCode: 422);
            }

            _logger.LogInformation("Order {OrderId} created for customer {CustomerId} with total {TotalAmount}",
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
