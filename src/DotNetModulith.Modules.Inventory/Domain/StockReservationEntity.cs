namespace DotNetModulith.Modules.Inventory.Domain;

/// <summary>
/// 库存预留状态
/// </summary>
public enum StockReservationStatus
{
    /// <summary>
    /// 已预留，尚未最终确认
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已确认消耗
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// 已释放回库存
    /// </summary>
    Released = 2
}

/// <summary>
/// 库存预留明细，记录订单对库存的精确占用
/// </summary>
public sealed class StockReservationEntity
{
    /// <summary>
    /// 预留记录唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 关联库存标识
    /// </summary>
    public Guid StockId { get; set; }

    /// <summary>
    /// 订单标识
    /// </summary>
    public string OrderId { get; set; } = null!;

    /// <summary>
    /// 产品标识
    /// </summary>
    public string ProductId { get; set; } = null!;

    /// <summary>
    /// 预留数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public StockReservationStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 最终状态变更时间
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
