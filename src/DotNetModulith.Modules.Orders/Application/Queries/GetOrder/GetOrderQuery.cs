using DotNetModulith.Modules.Orders.Domain;
using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Queries.GetOrder;

/// <summary>
/// 查询订单详情
/// </summary>
/// <param name="OrderId">订单ID</param>
public sealed record GetOrderQuery(OrderId OrderId) : IQuery<OrderDetail?>;
