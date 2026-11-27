using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeEventSettingConfiguration : IEntityTypeConfiguration<ShowtimeEventSetting>
{
    public void Configure(EntityTypeBuilder<ShowtimeEventSetting> builder)
    {
        builder.ToTable(
            "ShowtimeEventSettings",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_ShowtimeEventSettings_AdjustmentMode",
                    "\"AdjustmentMode\" IN ('Multiplier','Amount')"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeEventSettings_AdjustmentValue",
                    "(\"AdjustmentMode\" = 'Multiplier' AND \"AdjustmentValue\" > 0) OR (\"AdjustmentMode\" = 'Amount' AND \"AdjustmentValue\" <> 0)"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeEventSettings_MaxTicketsPerCustomer",
                    "\"MaxTicketsPerCustomer\" IS NULL OR \"MaxTicketsPerCustomer\" > 0"
                );
                t.HasCheckConstraint(
                    "CK_ShowtimeEventSettings_EarlyAccess",
                    "\"GeneralSaleStartTime\" IS NOT NULL OR (cardinality(\"EarlyAccessTierIds\") = 0 AND NOT \"EarlyAccessPaidMembers\" AND NOT \"EarlyAccessMovieInterest\")"
                );
            }
        );
        builder.HasKey(x => x.ShowtimeId);
        builder
            .Property(x => x.AdjustmentMode)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.AdjustmentValue).HasColumnType("decimal(18,4)");
        builder
            .Property(x => x.GeneralSaleStartTime)
            .HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.EarlyAccessTierIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.EarlyAccessPaidMembers).HasDefaultValue(false);
        builder.Property(x => x.EarlyAccessMovieInterest).HasDefaultValue(false);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Showtime)
            .WithOne(s => s.EventSetting)
            .HasForeignKey<ShowtimeEventSetting>(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
