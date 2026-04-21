namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单行项目实体
/// </summary>
public sealed class OrderLine
{
    /// <summary>
    /// 行项目唯一标识
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 产品ID
    /// </summary>
    public string ProductId { get; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; }

    /// <summary>
    /// 单价
    /// </summary>
    public decimal UnitPrice { get; }

    /// <summary>
    /// 行项目总金额
    /// </summary>
    public decimal LineTotal => Quantity * UnitPrice;

    public OrderLine(string productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be positive.");
        UnitPrice = unitPrice >= 0 ? unitPrice : throw new ArgumentException("Unit price must be non-negative.");
    }
}
