using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Results;

namespace DotNetModulith.Modules.Inventory.Api;

public interface IInventoryService
{
    Task<Result> CheckStockAsync(IReadOnlyList<CheckStockLine> lines, CancellationToken ct = default);
    Task<Result> ReserveStockAsync(string orderId, string customerId, decimal totalAmount, IReadOnlyList<ReserveStockLine> lines, CancellationToken ct = default);
}

public sealed record CheckStockLine(string ProductId, int Quantity);
public sealed record ReserveStockLine(string ProductId, int Quantity);
