namespace DotNetModulith.Modules.Inventory.Api.Contracts.Responses;

/// <summary>
/// 创建库存记录响应数据
/// </summary>
/// <param name="StockId">库存ID。</param>
public sealed record CreateStockResponse(string StockId);
