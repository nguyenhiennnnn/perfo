using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class DailyClosingConfiguration : IEntityTypeConfiguration<DailyClosing>
{
    public void Configure(EntityTypeBuilder<DailyClosing> builder)
    {
        builder.ToTable(
            "DailyClosings",
            t =>
            {
                t.HasCheckConstraint("CK_DailyClosings_Status", "\"Status\" IN ('Draft','Closed')");
                t.HasCheckConstraint(
                    "CK_DailyClosings_Closure",
                    "(\"Status\" = 'Draft' AND \"ClosedAt\" IS NULL AND \"ClosedByUserId\" IS NULL "
                        + "AND \"CountedCash\" IS NULL AND \"Variance\" IS NULL) OR "
                        + "(\"Status\" = 'Closed' AND \"SnapshotJson\" IS NOT NULL AND \"ClosedAt\" IS NOT NULL "
                        + "AND \"ClosedByUserId\" IS NOT NULL AND \"ExpectedCash\" IS NOT NULL "
                        + "AND \"CountedCash\" IS NOT NULL AND \"CountedCash\" >= 0 AND \"Variance\" IS NOT NULL "
                        + "AND \"Variance\" = \"CountedCash\" - \"ExpectedCash\")"
                );
            }
        );
        builder.HasKey(x => x.DailyClosingId);
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.DailyClosingStatus.Draft);
        builder.Property(x => x.SnapshotJson).HasColumnType("jsonb");
        builder.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CountedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Variance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.DailyClosingNote);
        builder.Property(x => x.ClosedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ReportEmailedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.ClosedByUser)
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.BusinessDate)
            .IsUnique()
            .HasDatabaseName("UX_DailyClosings_BusinessDate");
    }
}
