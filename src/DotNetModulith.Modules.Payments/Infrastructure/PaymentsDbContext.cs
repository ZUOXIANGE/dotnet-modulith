using DotNetModulith.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Payments.Infrastructure;

/// <summary>
/// 支付模块数据库上下文
/// </summary>
public sealed class PaymentsDbContext : DbContext
{
    /// <summary>
    /// 支付数据集
    /// </summary>
    public DbSet<Payment> Payments => Set<Payment>();

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

/// <summary>
/// 支付实体EF Core配置
/// </summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new PaymentId(value));

        builder.Property(p => p.OrderId).HasColumnName("order_id").HasMaxLength(100).IsRequired();
        builder.Property(p => p.CustomerId).HasColumnName("customer_id").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CompletedAt).HasColumnName("completed_at");

        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => p.OrderId).IsUnique();
    }
}
