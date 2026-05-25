namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单实体
/// </summary>
public sealed class OrderEntity
{
    /// <summary>
    /// 订单唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 租户标识，由 Finbuckle 在保存时强制写入。
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// 客户ID
    /// </summary>
    public string CustomerId { get; set; } = null!;

    /// <summary>
    /// 订单状态
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// 订单总金额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 行版本号，用于乐观并发控制
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;

    /// <summary>
    /// 订单行项目列表
    /// </summary>
    public List<OrderLineEntity> Lines { get; set; } = [];
}
