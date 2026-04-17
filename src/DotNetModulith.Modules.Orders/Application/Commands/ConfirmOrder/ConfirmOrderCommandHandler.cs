using System.Diagnostics;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Domain;
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

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<ConfirmOrderCommandHandler> logger,
        IFusionCache cache)
    {
        _orderRepository = orderRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
        _cache = cache;
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
            return Result.Failure($"Order {command.OrderId} not found.", "ORDER_NOT_FOUND");
        }

        try
        {
            order.Confirm();
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(order.Id.ToString()), null, cancellationToken);
            await _domainEventDispatcher.DispatchAsync(order, cancellationToken);

            _logger.LogInformation("Order {OrderId} confirmed", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to confirm order {OrderId}: {Reason}",
                command.OrderId, ex.Message);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure(ex.Message, "INVALID_STATE");
        }
    }
}
