using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeDailyStatConfiguration : IEntityTypeConfiguration<ShowtimeDailyStat>
{
    public void Configure(EntityTypeBuilder<ShowtimeDailyStat> builder)
    {
        builder.ToTable(
            "ShowtimeDailyStats",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_ShowtimeDailyStats_Kind",
                    "\"Kind\" IN ('Regular','Private','Event')"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeDailyStats_Counts",
                    "\"SellableSeats\" >= 0 AND \"TicketsSold\" >= 0 AND \"CancelledOrders\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeDailyStats_Amounts",
                    "\"GrossTicketRevenue\" >= 0 AND \"DiscountAmount\" >= 0 AND \"RefundAmount\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.ShowtimeId);
        builder.Property(x => x.ShowtimeId).ValueGeneratedNever();
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder
            .Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(ShowtimeKind.Regular);
        builder.Property(x => x.GrossTicketRevenue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ComboRevenue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Showtime)
            .WithOne()
            .HasForeignKey<ShowtimeDailyStat>(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.BusinessDate, x.Kind })
            .HasDatabaseName("IX_ShowtimeDailyStats_Date_Kind");
    }
}
