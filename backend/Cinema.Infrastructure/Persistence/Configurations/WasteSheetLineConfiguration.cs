using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class WasteSheetLineConfiguration : IEntityTypeConfiguration<WasteSheetLine>
{
    public void Configure(EntityTypeBuilder<WasteSheetLine> builder)
    {
        builder.ToTable(
            "WasteSheetLines",
            t =>
            {
                t.HasCheckConstraint("CK_WasteSheetLines_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_WasteSheetLines_UnitCost", "\"UnitCost\" >= 0");
            }
        );
        builder.HasKey(x => x.WasteSheetLineId);
        builder
            .HasIndex(x => new { x.WasteSheetId, x.InventoryItemId })
            .IsUnique()
            .HasDatabaseName("UX_WasteSheetLines_Sheet_Item");
        builder.HasIndex(x => x.InventoryItemId);
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");

        builder
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
