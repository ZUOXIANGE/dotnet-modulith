using DotNetModulith.Modules.Inventory.Application.Queries.GetStock;
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
    public static partial StockDetail ToDetail(this StockEntity stock);
}
