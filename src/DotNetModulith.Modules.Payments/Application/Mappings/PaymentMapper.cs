using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Modules.Payments.Domain;
using DotNetModulith.Modules.Payments.Domain.Events;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Payments.Application.Mappings;

/// <summary>
/// 支付模块对象映射器，使用Mapperly源生成器实现编译时映射代码生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PaymentMapper
{
    /// <summary>
    /// 将支付聚合根映射为支付详情DTO
    /// </summary>
    [MapProperty(nameof(Payment.Id), nameof(PaymentDetail.PaymentId))]
    [MapProperty(nameof(Payment.Status), nameof(PaymentDetail.Status))]
    public static partial PaymentDetail ToDetail(this Payment payment);

    /// <summary>
    /// 将支付ID转换为字符串
    /// </summary>
    private static string MapPaymentIdToString(PaymentId id) => id.ToString();

    /// <summary>
    /// 将支付状态枚举转换为字符串
    /// </summary>
    private static string MapPaymentStatusToString(PaymentStatus status) => status.ToString();

    /// <summary>
    /// 将支付完成领域事件映射为支付完成集成事件
    /// </summary>
    public static PaymentCompletedIntegrationEvent ToIntegrationEvent(
        this PaymentCompletedDomainEvent domainEvent) => new(
        domainEvent.OrderId,
        domainEvent.PaymentId.ToString(),
        domainEvent.CustomerId,
        domainEvent.Amount);

    /// <summary>
    /// 将支付失败领域事件映射为支付失败集成事件
    /// </summary>
    public static PaymentFailedIntegrationEvent ToIntegrationEvent(
        this PaymentFailedDomainEvent domainEvent) => new(
        domainEvent.OrderId,
        domainEvent.PaymentId.ToString(),
        domainEvent.CustomerId,
        domainEvent.Reason);
}

/// <summary>
/// 支付详情DTO
/// </summary>
/// <param name="PaymentId">支付ID</param>
/// <param name="OrderId">关联的订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Amount">支付金额</param>
/// <param name="Status">支付状态</param>
/// <param name="TransactionId">交易ID</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="CompletedAt">完成时间</param>
public sealed record PaymentDetail(
    string PaymentId,
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Status,
    string? TransactionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
