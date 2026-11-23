using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ComboRecipeLineConfiguration : IEntityTypeConfiguration<ComboRecipeLine>
{
    public void Configure(EntityTypeBuilder<ComboRecipeLine> builder)
    {
        builder.ToTable(
            "ComboRecipeLines",
            t => t.HasCheckConstraint("CK_ComboRecipeLines_Quantity", "\"Quantity\" > 0")
        );
        builder.HasKey(x => x.ComboRecipeLineId);
        builder
            .HasIndex(x => new { x.ComboRecipeId, x.InventoryItemId })
            .IsUnique()
            .HasDatabaseName("UX_ComboRecipeLines_Recipe_Item");
        builder.HasIndex(x => x.InventoryItemId);
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)");

        builder
            .HasOne(x => x.InventoryItem)
            .WithMany(i => i.RecipeLines)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
