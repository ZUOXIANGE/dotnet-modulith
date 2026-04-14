using System.Text.Json.Serialization;
using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Abstractions.Contracts.Inventory;

/// <summary>
/// 库存已预留集成事件
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="Lines">预留行项目列表</param>
public sealed record StockReservedIntegrationEvent(
    [property: JsonPropertyName("orderId")] string OrderId,
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
