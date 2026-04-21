using DotNetModulith.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Payments.Infrastructure;

/// <summary>
/// 支付模块数据库上下文
/// </summary>
public sealed class PaymentsDbContext : DbContext
{
    /// <summary>
    /// 支付数据集
    /// </summary>
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
