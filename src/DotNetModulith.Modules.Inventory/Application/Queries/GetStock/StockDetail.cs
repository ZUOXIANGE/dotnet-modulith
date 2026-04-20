namespace DotNetModulith.Modules.Inventory.Application.Queries.GetStock;

/// <summary>
/// 库存详情DTO
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="AvailableQuantity">可用数量</param>
/// <param name="ReservedQuantity">已预留数量</param>
public sealed record StockDetail(
    string ProductId,
    string ProductName,
    int AvailableQuantity,
    int ReservedQuantity);
