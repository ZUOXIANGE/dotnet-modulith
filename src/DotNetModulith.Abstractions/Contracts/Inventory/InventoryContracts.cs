using System.Text.Json.Serialization;
using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Abstractions.Contracts.Inventory;

/// <summary>
/// 库存已预留集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="CustomerId">客户ID</param>
/// <param name="TotalAmount">订单总金额</param>
/// <param name="Lines">预留行项目列表</param>
public sealed record StockReservedIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("lines")] IReadOnlyList<StockReservedLine> Lines) : IntegrationEvent;

/// <summary>
/// 库存预留行项目
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">预留数量</param>
public sealed record StockReservedLine(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("quantity")] int Quantity);

/// <summary>
/// 库存已补充集成事件
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">补充数量</param>
public sealed record StockReplenishedIntegrationEvent(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("quantity")] int Quantity) : IntegrationEvent;

/// <summary>
/// 库存不足集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="ProductId">产品ID</param>
/// <param name="Requested">请求预留数量</param>
/// <param name="Available">实际可用数量</param>
public sealed record StockInsufficientIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("requested")] int Requested,
    [property: JsonPropertyName("available")] int Available) : IntegrationEvent;

/// <summary>
/// 低库存预警集成事件
/// </summary>
/// <param name="Threshold">触发预警的库存阈值</param>
/// <param name="DetectedAt">检测时间（UTC）</param>
/// <param name="Items">命中的低库存明细</param>
public sealed record LowStockDetectedIntegrationEvent(
    [property: JsonPropertyName("threshold")] int Threshold,
    [property: JsonPropertyName("detectedAt")] DateTimeOffset DetectedAt,
    [property: JsonPropertyName("items")] IReadOnlyList<LowStockAlertItem> Items) : IntegrationEvent;

/// <summary>
/// 低库存预警明细
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="AvailableQuantity">当前可用库存</param>
/// <param name="ReservedQuantity">当前已预留库存</param>
public sealed record LowStockAlertItem(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("availableQuantity")] int AvailableQuantity,
    [property: JsonPropertyName("reservedQuantity")] int ReservedQuantity);
