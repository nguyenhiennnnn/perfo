using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ComboRecipeConfiguration : IEntityTypeConfiguration<ComboRecipe>
{
    public void Configure(EntityTypeBuilder<ComboRecipe> builder)
    {
        builder.ToTable("ComboRecipes");
        builder.HasKey(x => x.ComboRecipeId);
        builder.HasIndex(x => x.ComboId).IsUnique().HasDatabaseName("UX_ComboRecipes_ComboId");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Combo)
            .WithOne()
            .HasForeignKey<ComboRecipe>(x => x.ComboId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasMany(x => x.Lines)
            .WithOne(l => l.Recipe)
            .HasForeignKey(l => l.ComboRecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
