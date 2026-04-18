using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Inventory.Api.Contracts.Requests;

/// <summary>
/// 创建库存记录请求
/// </summary>
public sealed record CreateStockRequest
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
    /// 初始库存数量
    /// </summary>
    [Range(0, int.MaxValue)]
    public int InitialQuantity { get; init; }
}

/// <summary>
/// 补充库存请求
/// </summary>
public sealed record ReplenishStockRequest
{
    /// <summary>
    /// 补充数量
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
