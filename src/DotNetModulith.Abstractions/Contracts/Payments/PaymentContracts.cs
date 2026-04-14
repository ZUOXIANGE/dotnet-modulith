using System.Text.Json.Serialization;
using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Abstractions.Contracts.Payments;

/// <summary>
/// 支付完成集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="PaymentId">支付ID</param>
/// <param name="Amount">支付金额</param>
public sealed record PaymentCompletedIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("amount")] decimal Amount) : IntegrationEvent;

/// <summary>
/// 支付失败集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="PaymentId">支付ID</param>
/// <param name="Reason">失败原因</param>
public sealed record PaymentFailedIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("reason")] string Reason) : IntegrationEvent;
