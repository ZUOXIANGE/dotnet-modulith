using System.Diagnostics;
using DotNetModulith.Abstractions.Domain;
using DotNetModulith.Modules.Payments.Domain.Events;

namespace DotNetModulith.Modules.Payments.Domain;

/// <summary>
/// 支付ID值对象，封装支付的唯一标识
/// </summary>
/// <param name="Value">支付ID的GUID值</param>
public sealed record PaymentId(Guid Value)
{
    /// <summary>
    /// 生成新的支付ID
    /// </summary>
    public static PaymentId New() => new(Guid.NewGuid());

    /// <summary>
    /// 返回支付ID的字符串表示
    /// </summary>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// 支付状态枚举
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 已失败
    /// </summary>
    Failed = 2,

    /// <summary>
    /// 已退款
    /// </summary>
    Refunded = 3
}

/// <summary>
/// 支付聚合根，管理支付的生命周期和状态流转
/// </summary>
public sealed class PaymentEntity : AggregateRoot, IEntity<PaymentId>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Payments");

    /// <summary>
    /// 支付唯一标识
    /// </summary>
    public PaymentId Id { get; private set; } = null!;

    /// <summary>
    /// 关联的订单ID
    /// </summary>
    public string OrderId { get; private set; } = null!;

    /// <summary>
    /// 客户ID
    /// </summary>
    public string CustomerId { get; private set; } = null!;

    /// <summary>
    /// 支付金额
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// 支付状态
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// 交易ID（支付网关返回）
    /// </summary>
    public string? TransactionId { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    private PaymentEntity() { }

    /// <summary>
    /// 创建新的支付记录
    /// </summary>
    /// <param name="orderId">关联的订单ID</param>
    /// <param name="customerId">客户ID</param>
    /// <param name="amount">支付金额</param>
    /// <returns>新创建的支付实例</returns>
    public static PaymentEntity Create(string orderId, string customerId, decimal amount)
    {
        using var activity = ActivitySource.StartActivity("PaymentEntity.Create", ActivityKind.Internal);

        var payment = new PaymentEntity
        {
            Id = PaymentId.New(),
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        activity?.SetTag("modulith.order_id", orderId);
        activity?.SetTag("modulith.amount", amount);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return payment;
    }

    /// <summary>
    /// 完成支付，状态须为待处理
    /// </summary>
    /// <param name="transactionId">支付网关返回的交易ID</param>
    public void Complete(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot complete payment in status {Status}.");

        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        CompletedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PaymentCompletedDomainEvent(Id, OrderId, CustomerId, Amount));
    }

    /// <summary>
    /// 标记支付失败，状态须为待处理
    /// </summary>
    /// <param name="reason">失败原因</param>
    public void Fail(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment in status {Status}.");

        Status = PaymentStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PaymentFailedDomainEvent(Id, OrderId, CustomerId, reason));
    }
}
