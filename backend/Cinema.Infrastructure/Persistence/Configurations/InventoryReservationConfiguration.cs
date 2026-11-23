using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable(
            "InventoryReservations",
            t =>
            {
                t.HasCheckConstraint("CK_InventoryReservations_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_InventoryReservations_Status",
                    "\"Status\" IN ('Reserved','Committed','Released')"
                );
                t.HasCheckConstraint(
                    "CK_InventoryReservations_Resolved",
                    "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.InventoryReservationId);
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
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
            .WithMany(i => i.Reservations)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.OrderLineId, x.InventoryItemId })
            .IsUnique()
            .HasDatabaseName("UX_InventoryReservations_Line_Item");
        builder
            .HasIndex(x => new { x.OrderId, x.Status })
            .HasDatabaseName("IX_InventoryReservations_Order");
        builder
            .HasIndex(x => new { x.InventoryItemId, x.Status })
            .HasFilter("\"Status\" = 'Reserved'")
            .HasDatabaseName("IX_InventoryReservations_Reserved");
    }
}
