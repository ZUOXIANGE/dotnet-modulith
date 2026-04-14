namespace DotNetModulith.Modules.Orders.Application.Queries.GetOrder;

/// <summary>
/// 订单详情DTO
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Status">订单状态</param>
/// <param name="TotalAmount">订单总金额</param>
/// <param name="Lines">订单行项目列表</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="UpdatedAt">更新时间</param>
public sealed record OrderDetail(
    string OrderId,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderLineDetail> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// 订单行项目详情DTO
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">单价</param>
/// <param name="LineTotal">行项目总金额</param>
public sealed record OrderLineDetail(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
