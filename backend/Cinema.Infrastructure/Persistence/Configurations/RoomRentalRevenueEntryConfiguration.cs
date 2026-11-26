using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomRentalRevenueEntryConfiguration : IEntityTypeConfiguration<RoomRentalRevenueEntry>
{
    public void Configure(EntityTypeBuilder<RoomRentalRevenueEntry> builder)
    {
        builder.ToTable(
            "RoomRentalRevenueEntries",
            t => t.HasCheckConstraint("CK_RoomRentalRevenueEntries_Amount", "\"Amount\" > 0")
        );
        builder.HasKey(x => x.RoomRentalRevenueEntryId);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RoomRentalRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.RoomRentalRequestId)
            .IsUnique()
            .HasDatabaseName("UX_RoomRentalRevenueEntries_Request");
        builder.HasIndex(x => x.ShowtimeId).HasDatabaseName("IX_RoomRentalRevenueEntries_Showtime");
        builder.HasIndex(x => x.BusinessDate).HasDatabaseName("IX_RoomRentalRevenueEntries_Date");
    }
}
