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
public sealed class StockReservation
{
    /// <summary>
    /// 预留记录唯一标识
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联库存标识
    /// </summary>
    public StockId StockId { get; private set; } = null!;

    /// <summary>
    /// 订单标识
    /// </summary>
    public string OrderId { get; private set; } = null!;

    /// <summary>
    /// 产品标识
    /// </summary>
    public string ProductId { get; private set; } = null!;

    /// <summary>
    /// 预留数量
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public StockReservationStatus Status { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// 最终状态变更时间
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    private StockReservation()
    {
    }

    /// <summary>
    /// 创建新的库存预留明细
    /// </summary>
    public static StockReservation Create(StockId stockId, string orderId, string productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        return new StockReservation
        {
            Id = Guid.NewGuid(),
            StockId = stockId,
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            Status = StockReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 确认预留，表示库存已被最终消耗
    /// </summary>
    public void Confirm()
    {
        if (Status != StockReservationStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm reservation in status {Status}.");

        Status = StockReservationStatus.Confirmed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 释放预留，表示库存退回可用池
    /// </summary>
    public void Release()
    {
        if (Status != StockReservationStatus.Pending)
            throw new InvalidOperationException($"Cannot release reservation in status {Status}.");

        Status = StockReservationStatus.Released;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
