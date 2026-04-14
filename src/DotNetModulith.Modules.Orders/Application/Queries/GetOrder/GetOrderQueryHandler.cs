using System.Diagnostics;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Queries.GetOrder;

/// <summary>
/// 查询订单详情处理器
/// </summary>
public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, OrderDetail?>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async ValueTask<OrderDetail?> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetOrder", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", query.OrderId.ToString());

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order is null)
            return null;

        return new OrderDetail(
            order.Id.ToString(),
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Lines.Select(l => new OrderLineDetail(
                l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
            order.CreatedAt,
            order.UpdatedAt);
    }
}
