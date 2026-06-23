using DotNetModulith.Modules.Fines.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Fines.Infrastructure.Configurations;

public sealed class FineEntityConfiguration : IEntityTypeConfiguration<FineEntity>
{
    public void Configure(EntityTypeBuilder<FineEntity> builder)
    {
        builder.ToTable("fines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.BorrowingRecordId).IsRequired(false);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.PaidAt).IsRequired(false);

        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.MemberId, x.Status });
    }
}
