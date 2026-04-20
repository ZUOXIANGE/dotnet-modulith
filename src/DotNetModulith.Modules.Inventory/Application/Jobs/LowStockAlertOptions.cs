using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Inventory.Application.Jobs;

/// <summary>
/// 库存预警扫描配置
/// </summary>
public sealed class LowStockAlertOptions
{
    public const string SectionName = "InventoryAlert";

    /// <summary>
    /// 低库存阈值，小于等于该值时触发预警
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Threshold { get; set; } = 10;

    /// <summary>
    /// 单次扫描最多处理的低库存记录数
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;
}
