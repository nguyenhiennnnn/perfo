using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(
            "InventoryItems",
            t =>
            {
                t.HasCheckConstraint("CK_InventoryItems_QuantityOnHand", "\"QuantityOnHand\" >= 0");
                t.HasCheckConstraint(
                    "CK_InventoryItems_ReservedQuantity",
                    "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"QuantityOnHand\""
                );
                t.HasCheckConstraint("CK_InventoryItems_UnitCost", "\"UnitCost\" >= 0");
                t.HasCheckConstraint(
                    "CK_InventoryItems_LowStockThreshold",
                    "\"LowStockThreshold\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.InventoryItemId);
        builder.Property(x => x.Code).HasMaxLength(FieldLengths.InventoryItemCode).IsRequired();
        builder
            .HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_InventoryItems_Code");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.InventoryItemName).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(FieldLengths.InventoryUnit).IsRequired();
        builder.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,3)").HasDefaultValue(0m);
        builder
            .Property(x => x.ReservedQuantity)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder
            .Property(x => x.LowStockThreshold)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);
        builder
            .Property(x => x.LowStockAlertedAt)
            .HasColumnType("timestamp without time zone");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
