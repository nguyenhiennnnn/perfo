using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class StockReceiptLineConfiguration : IEntityTypeConfiguration<StockReceiptLine>
{
    public void Configure(EntityTypeBuilder<StockReceiptLine> builder)
    {
        builder.ToTable(
            "StockReceiptLines",
            t =>
            {
                t.HasCheckConstraint("CK_StockReceiptLines_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_StockReceiptLines_UnitCost", "\"UnitCost\" >= 0");
            }
        );
        builder.HasKey(x => x.StockReceiptLineId);
        builder.HasIndex(x => x.StockReceiptId);
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
