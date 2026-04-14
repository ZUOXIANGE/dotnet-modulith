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

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsTracking()
            .Include("_lines")
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include("_lines")
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .Include("_lines")
            .Where(o => o.Status == OrderStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }
}
