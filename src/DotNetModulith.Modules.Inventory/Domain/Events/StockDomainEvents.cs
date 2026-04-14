using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Inventory.Domain;

namespace DotNetModulith.Modules.Inventory.Domain.Events;

/// <summary>
/// 库存已预留领域事件
/// </summary>
/// <param name="StockId">库存ID</param>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">预留数量</param>
public sealed record StockReservedDomainEvent(StockId StockId, string ProductId, int Quantity) : DomainEvent;

/// <summary>
/// 库存已释放领域事件
/// </summary>
/// <param name="StockId">库存ID</param>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">释放数量</param>
public sealed record StockReleasedDomainEvent(StockId StockId, string ProductId, int Quantity) : DomainEvent;

/// <summary>
/// 库存已补充领域事件
/// </summary>
/// <param name="StockId">库存ID</param>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">补充数量</param>
public sealed record StockReplenishedDomainEvent(StockId StockId, string ProductId, int Quantity) : DomainEvent;
