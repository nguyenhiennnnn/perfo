using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class WasteSheetConfiguration : IEntityTypeConfiguration<WasteSheet>
{
    public void Configure(EntityTypeBuilder<WasteSheet> builder)
    {
        builder.ToTable(
            "WasteSheets",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_WasteSheets_Reason",
                    "\"Reason\" IN ('Expired','Damaged','Spilled','Other')"
                );
                t.HasCheckConstraint(
                    "CK_WasteSheets_OtherNote",
                    "\"Reason\" <> 'Other' OR (\"Note\" IS NOT NULL AND btrim(\"Note\") <> '')"
                );
            }
        );
        builder.HasKey(x => x.WasteSheetId);
        builder.Property(x => x.WasteDate).HasColumnType("date");
        builder.HasIndex(x => x.WasteDate);
        builder
            .Property(x => x.Reason)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.StockNote);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasMany(x => x.Lines)
            .WithOne(l => l.Sheet)
            .HasForeignKey(l => l.WasteSheetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
