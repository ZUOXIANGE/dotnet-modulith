namespace DotNetModulith.Modules.Payments.Domain;

/// <summary>
/// 支付实体
/// </summary>
public sealed class PaymentEntity
{
    /// <summary>
    /// 支付唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 关联的订单ID
    /// </summary>
    public string OrderId { get; set; } = null!;

    /// <summary>
    /// 客户ID
    /// </summary>
    public string CustomerId { get; set; } = null!;

    /// <summary>
    /// 支付金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 支付状态
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// 交易ID（支付网关返回）
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// 行版本号，用于乐观并发控制
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}
