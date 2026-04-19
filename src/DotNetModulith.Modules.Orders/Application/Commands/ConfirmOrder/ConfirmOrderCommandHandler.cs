using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using Mediator;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;

/// <summary>
/// 确认订单命令处理器
/// </summary>
public sealed class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly IOrderRepository _orderRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<ConfirmOrderCommandHandler> _logger;
    private readonly IFusionCache _cache;
    private readonly OrdersDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<ConfirmOrderCommandHandler> logger,
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

    public async ValueTask<Result> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ConfirmOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", command.OrderId.ToString());

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for confirmation",
                command.OrderId);

            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            throw new BusinessException(
                message: $"Order {command.OrderId} not found.",
                code: ApiCodes.Common.NotFound,
                httpStatusCode: 404);
        }

        try
        {
            order.Confirm();
            await CapTransactionScope.ExecuteAsync(
                _dbContext,
                _capPublisher,
                async ct =>
                {
                    await _orderRepository.UpdateAsync(order, ct);
                    await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, ct);
                    await _domainEventDispatcher.DispatchAsync(order, ct);
                },
                cancellationToken);

            _logger.LogInformation("Order {OrderId} confirmed", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to confirm order {OrderId}: {Reason}",
                command.OrderId, ex.Message);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw new BusinessException(
                message: ex.Message,
                code: ApiCodes.Order.InvalidState,
                httpStatusCode: 400,
                innerException: ex);
        }
    }
}
