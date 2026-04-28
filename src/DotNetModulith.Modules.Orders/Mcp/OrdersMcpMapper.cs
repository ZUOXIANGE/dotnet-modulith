using DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;
using DotNetModulith.Modules.Orders.Application.Queries.GetOrder;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Mcp.Contracts;

namespace DotNetModulith.Modules.Orders.Mcp;

internal static class OrdersMcpMapper
{
    public static CreateOrderCommand ToCommand(CreateOrderToolRequest request)
        => new(
            request.CustomerId,
            request.Lines
                .Select(line => new OrderLineData(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice))
                .ToArray());

    public static OrderDetailToolResult ToToolResult(OrderDetail detail)
        => new(
            detail.OrderId,
            detail.CustomerId,
            detail.Status,
            detail.TotalAmount,
            detail.Lines.Select(ToToolResult).ToArray(),
            detail.CreatedAt,
            detail.UpdatedAt);

    private static OrderLineToolResult ToToolResult(OrderLineDetail detail)
        => new(
            detail.ProductId,
            detail.ProductName,
            detail.Quantity,
            detail.UnitPrice,
            detail.LineTotal);
}