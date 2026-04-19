using System.Text.Json.Serialization;
using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Abstractions.Contracts.Orders;

/// <summary>
/// 订单已创建集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="TotalAmount">订单总金额</param>
/// <param name="Lines">订单行项目列表</param>
public sealed record OrderCreatedIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("lines")] IReadOnlyList<OrderLineContract> Lines) : IntegrationEvent;

/// <summary>
/// 订单行项目契约
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">单价</param>
public sealed record OrderLineContract(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// 订单已支付集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="TotalAmount">支付金额</param>
public sealed record OrderPaidIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount) : IntegrationEvent;

/// <summary>
/// 订单已取消集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="Reason">取消原因</param>
/// <param name="Lines">订单行项目列表</param>
public sealed record OrderCancelledIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("lines")] IReadOnlyList<OrderLineContract> Lines) : IntegrationEvent;
