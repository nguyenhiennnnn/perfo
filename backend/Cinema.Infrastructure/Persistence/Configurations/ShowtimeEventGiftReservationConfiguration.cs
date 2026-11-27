using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeEventGiftReservationConfiguration
    : IEntityTypeConfiguration<ShowtimeEventGiftReservation>
{
    public void Configure(EntityTypeBuilder<ShowtimeEventGiftReservation> builder)
    {
        builder.ToTable(
            "ShowtimeEventGiftReservations",
            t =>
            {
                t.HasCheckConstraint("CK_ShowtimeEventGiftReservations_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_ShowtimeEventGiftReservations_Counts",
                    "\"QuantityGiven\" >= 0 AND \"QuantityReturned\" >= 0 AND \"QuantityGiven\" + \"QuantityReturned\" <= \"Quantity\""
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeEventGiftReservations_Status",
                    "\"Status\" IN ('Reserved','Committed','Released')"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeEventGiftReservations_Resolved",
                    "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.ShowtimeEventGiftReservationId);
        builder.Property(x => x.QuantityGiven).HasDefaultValue(0);
        builder.Property(x => x.QuantityReturned).HasDefaultValue(0);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.ResourceHoldStatus.Reserved);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.OrderLineId, x.InventoryItemId })
            .IsUnique()
            .HasDatabaseName("UX_ShowtimeEventGiftReservations_Line_Item");
        builder
            .HasIndex(x => new { x.OrderId, x.Status })
            .HasDatabaseName("IX_ShowtimeEventGiftReservations_Order");
        builder
            .HasIndex(x => new { x.ShowtimeId, x.InventoryItemId, x.Status })
            .HasDatabaseName("IX_ShowtimeEventGiftReservations_Gift");
    }
}
