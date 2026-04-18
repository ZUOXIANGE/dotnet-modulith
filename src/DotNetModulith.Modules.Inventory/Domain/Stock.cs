using System.Diagnostics;
using DotNetModulith.Abstractions.Domain;
using DotNetModulith.Modules.Inventory.Domain.Events;

namespace DotNetModulith.Modules.Inventory.Domain;

/// <summary>
/// 库存ID值对象，封装库存的唯一标识
/// </summary>
/// <param name="Value">库存ID的GUID值</param>
public sealed record StockId(Guid Value)
{
    /// <summary>
    /// 生成新的库存ID
    /// </summary>
    public static StockId New() => new(Guid.NewGuid());

    /// <summary>
    /// 返回库存ID的字符串表示
    /// </summary>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// 库存聚合根，管理产品的库存数量和预留逻辑
/// </summary>
public sealed class Stock : AggregateRoot, IEntity<StockId>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    /// <summary>
    /// 库存唯一标识
    /// </summary>
    public StockId Id { get; private set; } = null!;

    /// <summary>
    /// 产品ID
    /// </summary>
    public string ProductId { get; private set; } = null!;

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; private set; } = null!;

    /// <summary>
    /// 可用数量（未预留的库存）
    /// </summary>
    public int AvailableQuantity { get; private set; }

    /// <summary>
    /// 已预留数量（已被订单锁定但尚未确认的库存）
    /// </summary>
    public int ReservedQuantity { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Stock() { }

    /// <summary>
    /// 创建新的库存记录
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <param name="productName">产品名称</param>
    /// <param name="initialQuantity">初始库存数量</param>
    /// <returns>新创建的库存实例</returns>
    public static Stock Create(string productId, string productName, int initialQuantity)
    {
        using var activity = ActivitySource.StartActivity("Stock.Create", ActivityKind.Internal);

        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID is required.", nameof(productId));

        var stock = new Stock
        {
            Id = StockId.New(),
            ProductId = productId,
            ProductName = productName,
            AvailableQuantity = initialQuantity,
            ReservedQuantity = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        activity?.SetTag("modulith.product_id", stock.ProductId);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return stock;
    }

    /// <summary>
    /// 尝试预留库存，可用数量不足时返回false
    /// </summary>
    /// <param name="quantity">预留数量</param>
    /// <returns>预留是否成功</returns>
    public bool TryReserve(int quantity)
    {
        if (AvailableQuantity < quantity)
            return false;

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new StockReservedDomainEvent(Id, ProductId, quantity));

        return true;
    }

    /// <summary>
    /// 释放已预留的库存（如订单取消时）
    /// </summary>
    /// <param name="quantity">释放数量</param>
    public void Release(int quantity)
    {
        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new StockReleasedDomainEvent(Id, ProductId, quantity));
    }

    /// <summary>
    /// 确认预留，将预留数量从已预留中扣除（如订单支付完成后）
    /// </summary>
    /// <param name="quantity">确认数量</param>
    public void ConfirmReservation(int quantity)
    {
        ReservedQuantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 补充库存
    /// </summary>
    /// <param name="quantity">补充数量</param>
    public void Replenish(int quantity)
    {
        AvailableQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new StockReplenishedDomainEvent(Id, ProductId, quantity));
    }
}
