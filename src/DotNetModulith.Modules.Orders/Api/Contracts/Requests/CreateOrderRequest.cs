using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Orders.Api.Contracts.Requests;

/// <summary>
/// 创建订单请求
/// </summary>
public sealed record CreateOrderRequest
{
    /// <summary>
    /// 客户ID
    /// </summary>
    [NotWhiteSpace]
    [StringLength(100)]
    public required string CustomerId { get; init; }

    /// <summary>
    /// 订单行项目列表
    /// </summary>
    [NotEmptyCollection]
    public required IReadOnlyList<CreateOrderLineRequest> Lines { get; init; }
}

/// <summary>
/// 创建订单行项目请求
/// </summary>
public sealed record CreateOrderLineRequest
{
    /// <summary>
    /// 产品ID
    /// </summary>
    [NotWhiteSpace]
    [StringLength(100)]
    public required string ProductId { get; init; }

    /// <summary>
    /// 产品名称
    /// </summary>
    [NotWhiteSpace]
    [StringLength(500)]
    public required string ProductName { get; init; }

    /// <summary>
    /// 数量
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    /// <summary>
    /// 单价
    /// </summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal UnitPrice { get; init; }
}
