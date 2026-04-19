using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Modules.Orders.Domain.Events;

/// <summary>
/// 订单已创建领域事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="TotalAmount">订单总金额</param>
/// <param name="Lines">订单行项目列表</param>
public sealed record OrderCreatedDomainEvent(
    OrderId OrderId,
    string CustomerId,
    decimal TotalAmount,
    IReadOnlyList<OrderLineData> Lines) : DomainEvent;

/// <summary>
/// 订单已支付领域事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="TotalAmount">支付金额</param>
public sealed record OrderPaidDomainEvent(
    OrderId OrderId,
    string CustomerId,
    decimal TotalAmount) : DomainEvent;

/// <summary>
/// 订单已取消领域事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Reason">取消原因</param>
/// <param name="Lines">订单行项目列表</param>
public sealed record OrderCancelledDomainEvent(
    OrderId OrderId,
    string CustomerId,
    string Reason,
    IReadOnlyList<OrderLineData> Lines) : DomainEvent;
