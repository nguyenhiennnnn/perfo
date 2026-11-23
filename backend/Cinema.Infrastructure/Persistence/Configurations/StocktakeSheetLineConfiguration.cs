using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class StocktakeSheetLineConfiguration : IEntityTypeConfiguration<StocktakeSheetLine>
{
    public void Configure(EntityTypeBuilder<StocktakeSheetLine> builder)
    {
        builder.ToTable(
            "StocktakeSheetLines",
            t =>
            {
                t.HasCheckConstraint("CK_StocktakeSheetLines_BookQuantity", "\"BookQuantity\" >= 0");
                t.HasCheckConstraint(
                    "CK_StocktakeSheetLines_CountedQuantity",
                    "\"CountedQuantity\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_StocktakeSheetLines_Difference",
                    "\"Difference\" = \"CountedQuantity\" - \"BookQuantity\""
                );
            }
        );
        builder.HasKey(x => x.StocktakeSheetLineId);
        builder
            .HasIndex(x => new { x.StocktakeSheetId, x.InventoryItemId })
            .IsUnique()
            .HasDatabaseName("UX_StocktakeSheetLines_Sheet_Item");
        builder.HasIndex(x => x.InventoryItemId);
        builder.Property(x => x.BookQuantity).HasColumnType("decimal(18,3)");
        builder.Property(x => x.CountedQuantity).HasColumnType("decimal(18,3)");
        builder.Property(x => x.Difference).HasColumnType("decimal(18,3)");

        builder
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
