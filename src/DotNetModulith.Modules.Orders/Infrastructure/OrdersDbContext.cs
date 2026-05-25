using DotNetModulith.Modules.Orders.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Orders.Infrastructure;

/// <summary>
/// 订单模块数据库上下文
/// </summary>
public sealed class OrdersDbContext : DbContext, IMultiTenantDbContext
{
    /// <summary>
    /// 订单数据集
    /// </summary>
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : this(options, tenantInfo: null)
    {
    }

    public OrdersDbContext(
        DbContextOptions<OrdersDbContext> options,
        IMultiTenantContextAccessor multiTenantContextAccessor)
        : this(options, multiTenantContextAccessor.MultiTenantContext?.TenantInfo)
    {
    }

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, ITenantInfo? tenantInfo) : base(options)
    {
        TenantInfo = tenantInfo;
    }

    public ITenantInfo? TenantInfo { get; }

    public TenantMismatchMode TenantMismatchMode => TenantMismatchMode.Throw;

    public TenantNotSetMode TenantNotSetMode => TenantNotSetMode.Throw;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        modelBuilder.Entity<OrderEntity>().IsMultiTenant().AdjustIndexes();
        modelBuilder.Entity<OrderLineEntity>().IsMultiTenant().AdjustIndexes();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
