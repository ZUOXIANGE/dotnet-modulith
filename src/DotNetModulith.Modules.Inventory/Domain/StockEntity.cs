namespace DotNetModulith.Modules.Inventory.Domain;

/// <summary>
/// 库存实体
/// </summary>
public sealed class StockEntity
{
    /// <summary>
    /// 库存唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 产品ID
    /// </summary>
    public string ProductId { get; set; } = null!;

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// 可用数量（未预留的库存）
    /// </summary>
    public int AvailableQuantity { get; set; }

    /// <summary>
    /// 已预留数量（已被订单锁定但尚未确认的库存）
    /// </summary>
    public int ReservedQuantity { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 最近一次发送低库存预警的时间
    /// </summary>
    public DateTimeOffset? LowStockAlertSentAt { get; set; }

    /// <summary>
    /// 最近一次发送低库存预警时的可用库存快照
    /// </summary>
    public int? LastAlertedAvailableQuantity { get; set; }

    /// <summary>
    /// 行版本号，用于乐观并发控制
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}
