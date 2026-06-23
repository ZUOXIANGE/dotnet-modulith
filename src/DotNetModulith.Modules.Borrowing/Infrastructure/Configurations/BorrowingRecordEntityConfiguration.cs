using DotNetModulith.Modules.Borrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Borrowing.Infrastructure.Configurations;

public sealed class BorrowingRecordEntityConfiguration : IEntityTypeConfiguration<BorrowingRecordEntity>
{
    public void Configure(EntityTypeBuilder<BorrowingRecordEntity> builder)
    {
        builder.ToTable("borrowing_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BookId).IsRequired();

        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.BorrowDate).IsRequired();

        builder.Property(x => x.DueDate).IsRequired();

        builder.Property(x => x.ReturnDate);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RenewalCount).IsRequired();

        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DueDate);
    }
}
