using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Orders.Mcp.Contracts;

public sealed record CreateOrderToolRequest
{
    [Description("客户标识")]
    [NotWhiteSpace]
    [StringLength(100)]
    public required string CustomerId { get; init; }

    [Description("订单行列表")]
    [NotEmptyCollection]
    public required IReadOnlyList<CreateOrderToolLineRequest> Lines { get; init; }
}

public sealed record CreateOrderToolLineRequest
{
    [Description("商品标识")]
    [NotWhiteSpace]
    [StringLength(100)]
    public required string ProductId { get; init; }

    [Description("商品名称")]
    [NotWhiteSpace]
    [StringLength(500)]
    public required string ProductName { get; init; }

    [Description("购买数量")]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [Description("商品单价")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal UnitPrice { get; init; }
}

public sealed record ConfirmOrderToolRequest
{
    [Description("订单标识")]
    public required Guid OrderId { get; init; }
}

public sealed record GetOrderToolRequest
{
    [Description("订单标识")]
    public required Guid OrderId { get; init; }
}

public sealed record OrderToolResult(
    string OrderId,
    string Status,
    string Message);

public sealed record OrderDetailToolResult(
    string OrderId,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderLineToolResult> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record OrderLineToolResult(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);