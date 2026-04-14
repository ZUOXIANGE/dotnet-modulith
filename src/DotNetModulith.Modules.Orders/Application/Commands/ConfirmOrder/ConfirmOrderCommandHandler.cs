using System.Diagnostics;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Events;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;

/// <summary>
/// 确认订单命令处理器
/// </summary>
public sealed class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly IOrderRepository _orderRepository;
    private readonly DomainEventToIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<ConfirmOrderCommandHandler> _logger;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        DomainEventToIntegrationEventPublisher eventPublisher,
        ILogger<ConfirmOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async ValueTask<Result> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ConfirmOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", command.OrderId.ToString());

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        if (order is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            return Result.Failure($"Order {command.OrderId} not found.", "ORDER_NOT_FOUND");
        }

        try
        {
            order.Confirm();
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _eventPublisher.PublishAsync(order, cancellationToken);
            order.ClearDomainEvents();

            _logger.LogInformation("Order {OrderId} confirmed", order.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure(ex.Message, "INVALID_STATE");
        }
    }
}
