namespace DotNetModulith.Modules.Payments.Application.Models;

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
