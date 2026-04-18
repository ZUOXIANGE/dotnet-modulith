namespace DotNetModulith.Modules.Orders.Api.Contracts.Responses;

/// <summary>
/// 创建订单响应数据
/// </summary>
/// <param name="OrderId">订单ID。</param>
public sealed record CreateOrderResponse(string OrderId);
