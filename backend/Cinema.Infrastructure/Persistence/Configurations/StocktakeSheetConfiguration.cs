using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class StocktakeSheetConfiguration : IEntityTypeConfiguration<StocktakeSheet>
{
    public void Configure(EntityTypeBuilder<StocktakeSheet> builder)
    {
        builder.ToTable("StocktakeSheets");
        builder.HasKey(x => x.StocktakeSheetId);
        builder.Property(x => x.StocktakeDate).HasColumnType("date");
        builder.HasIndex(x => x.StocktakeDate);
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.StockNote);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasMany(x => x.Lines)
            .WithOne(l => l.Sheet)
            .HasForeignKey(l => l.StocktakeSheetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
