using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Queries.GetOrder;

/// <summary>
/// 获取订单查询
/// </summary>
/// <param name="OrderId">订单ID</param>
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDetail?>;
