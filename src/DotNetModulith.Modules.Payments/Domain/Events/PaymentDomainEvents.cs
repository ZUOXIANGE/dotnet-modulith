using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Modules.Payments.Domain.Events;

/// <summary>
/// 支付完成领域事件
/// </summary>
/// <param name="PaymentId">支付ID</param>
/// <param name="OrderId">关联的订单ID</param>
/// <param name="TenantIdentifier">租户标识</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Amount">支付金额</param>
public sealed record PaymentCompletedDomainEvent(
    Guid PaymentId,
    string OrderId,
    string TenantIdentifier,
    string CustomerId,
    decimal Amount) : DomainEvent;

/// <summary>
/// 支付失败领域事件
/// </summary>
/// <param name="PaymentId">支付ID</param>
/// <param name="OrderId">关联的订单ID</param>
/// <param name="TenantIdentifier">租户标识</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Reason">失败原因</param>
public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    string OrderId,
    string TenantIdentifier,
    string CustomerId,
    string Reason) : DomainEvent;
