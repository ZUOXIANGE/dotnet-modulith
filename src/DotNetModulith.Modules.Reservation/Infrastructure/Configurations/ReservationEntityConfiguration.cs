using DotNetModulith.Modules.Reservation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Reservation.Infrastructure.Configurations;

public sealed class ReservationEntityConfiguration : IEntityTypeConfiguration<ReservationEntity>
{
    public void Configure(EntityTypeBuilder<ReservationEntity> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BookId).IsRequired();

        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.ReserveDate).IsRequired();

        builder.Property(x => x.ExpiryDate).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.QueuePosition).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.BookId, x.Status });
    }
}
