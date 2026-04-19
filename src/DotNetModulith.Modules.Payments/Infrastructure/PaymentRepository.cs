using DotNetModulith.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Payments.Infrastructure;

/// <summary>
/// 支付仓储实现，基于EF Core进行数据持久化
/// </summary>
internal sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;

    public PaymentRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default)
    {
        return await _context.Payments
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken ct = default)
    {
        return await _context.Payments
            .AsTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
    }

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        await _context.Payments.AddAsync(payment, ct);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Update(payment);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
