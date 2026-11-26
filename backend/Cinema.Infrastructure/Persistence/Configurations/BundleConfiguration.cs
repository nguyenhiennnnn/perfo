using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class BundleConfiguration : IEntityTypeConfiguration<Bundle>
{
    public void Configure(EntityTypeBuilder<Bundle> builder)
    {
        builder.ToTable(
            "Bundles",
            t =>
            {
                t.HasCheckConstraint("CK_Bundles_Price", "\"Price\" > 0");
                t.HasCheckConstraint(
                    "CK_Bundles_ValidRange",
                    "\"ValidFrom\" IS NULL OR \"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\""
                );
                t.HasCheckConstraint(
                    "CK_Bundles_TimeWindow",
                    "(\"AllowedFromTime\" IS NULL AND \"AllowedToTime\" IS NULL) "
                        + "OR (\"AllowedFromTime\" IS NOT NULL AND \"AllowedToTime\" IS NOT NULL "
                        + "AND \"AllowedToTime\" > \"AllowedFromTime\")"
                );
                t.HasCheckConstraint(
                    "CK_Bundles_DailyLimit",
                    "\"DailyLimit\" IS NULL OR \"DailyLimit\" > 0"
                );
            }
        );
        builder.HasKey(x => x.BundleId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_Bundles_Name");
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.ComboDescription);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ValidFrom).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ValidTo).HasColumnType("timestamp without time zone");
        builder.Property(x => x.MovieIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.ShowtimeIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.DaysOfWeek).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.SeatTypeIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.AllowedFromTime).HasColumnType("time");
        builder.Property(x => x.AllowedToTime).HasColumnType("time");
        builder.Property(x => x.ExcludesPromotions).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasMany(x => x.Items)
            .WithOne(i => i.Bundle)
            .HasForeignKey(i => i.BundleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
