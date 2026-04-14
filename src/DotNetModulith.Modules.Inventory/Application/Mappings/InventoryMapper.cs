using DotNetModulith.Modules.Inventory.Domain;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Inventory.Application.Mappings;

/// <summary>
/// 库存模块对象映射器，使用Mapperly源生成器实现编译时映射代码生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class InventoryMapper
{
    /// <summary>
    /// 将库存聚合根映射为库存详情DTO
    /// </summary>
    public static partial StockDetail ToDetail(this Stock stock);
}

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
