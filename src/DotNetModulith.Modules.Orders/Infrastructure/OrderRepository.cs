using DotNetModulith.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Orders.Infrastructure;

/// <summary>
/// 订单仓储实现，基于EF Core进行数据持久化
/// </summary>
internal sealed class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;

    public OrderRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetByCustomerIdAsync(string customerId, int limit, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetPendingOrdersAsync(int limit, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Lines)
            .Where(o => o.Status == OrderStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }

    public Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
