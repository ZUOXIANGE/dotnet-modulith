using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Domain;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;

/// <summary>
/// 预留库存命令
/// </summary>
/// <param name="OrderId">订单ID</param>
/// <param name="Lines">预留库存行项目列表</param>
public sealed record ReserveStockCommand(
    string OrderId,
    IReadOnlyList<ReserveStockLine> Lines) : ICommand<Result>;

/// <summary>
/// 预留库存行项目
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">预留数量</param>
public sealed record ReserveStockLine(string ProductId, int Quantity);
