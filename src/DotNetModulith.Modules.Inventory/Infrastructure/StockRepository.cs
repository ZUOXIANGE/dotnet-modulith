using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

/// <summary>
/// 库存仓储实现，基于EF Core进行数据持久化
/// </summary>
internal sealed class StockRepository : IStockRepository
{
    private readonly InventoryDbContext _context;

    public StockRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByIdAsync(StockId id, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Stock?> GetByProductIdAsync(string productId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsTracking()
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);
    }

    public async Task<IReadOnlyList<Stock>> GetLowStockAsync(int threshold, int limit, CancellationToken ct = default)
    {
        return await _context.Stocks
            .Where(s => s.AvailableQuantity <= threshold)
            .OrderBy(s => s.AvailableQuantity)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Stock stock, CancellationToken ct = default)
    {
        await _context.Stocks.AddAsync(stock, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Stock stock, CancellationToken ct = default)
    {
        _context.Stocks.Update(stock);
        await _context.SaveChangesAsync(ct);
    }
}
