using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeSalesPaceConfiguration : IEntityTypeConfiguration<ShowtimeSalesPace>
{
    public void Configure(EntityTypeBuilder<ShowtimeSalesPace> builder)
    {
        builder.ToTable(
            "ShowtimeSalesPaces",
            t =>
            {
                t.HasCheckConstraint("CK_ShowtimeSalesPaces_Mark", "\"MarkMinutesBeforeStart\" > 0");
                t.HasCheckConstraint(
                    "CK_ShowtimeSalesPaces_Counts",
                    "\"TicketsSold\" >= 0 AND \"SellableSeats\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeSalesPaces_Percent",
                    "\"SoldPercent\" >= 0 AND \"SoldPercent\" <= 100"
                );
            }
        );
        builder.HasKey(x => new { x.ShowtimeId, x.MarkMinutesBeforeStart });
        builder.Property(x => x.SoldPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => x.MarkMinutesBeforeStart)
            .HasDatabaseName("IX_ShowtimeSalesPaces_Mark");
    }
}
