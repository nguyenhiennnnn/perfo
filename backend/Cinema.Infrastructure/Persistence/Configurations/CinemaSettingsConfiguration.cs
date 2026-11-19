using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CinemaSettingsConfiguration : IEntityTypeConfiguration<CinemaSettings>
{
    public void Configure(EntityTypeBuilder<CinemaSettings> builder)
    {
        builder.ToTable(
            "CinemaSettings",
            t => t.HasCheckConstraint("CK_CinemaSettings_Id", "\"Id\" = 1")
        );
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.CinemaName).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(FieldLengths.CinemaAddress).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(FieldLengths.Phone);
        builder.Property(x => x.Email).HasMaxLength(FieldLengths.Email);
        builder.Property(x => x.TaxCode).HasMaxLength(FieldLengths.Name);
        builder.Property(x => x.LogoUrl).HasMaxLength(FieldLengths.Url);
        builder
            .Property(x => x.DefaultEmployeePassword)
            .HasMaxLength(FieldLengths.DefaultEmployeePassword)
            .IsRequired()
            .HasDefaultValue(CinemaSettings.DefaultEmployeePasswordValue);
        builder.Property(x => x.OpeningTime).HasColumnType("time");
        builder.Property(x => x.ClosingTime).HasColumnType("time");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.PointsPerAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(10000m);
        builder
            .Property(x => x.CancelRefundPercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(100m);
        builder
            .Property(x => x.SeatGapRuleMode)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.SeatGapRuleMode.Strict);

        builder
            .Property(x => x.RoomBufferMinutes)
            .HasDefaultValue(CinemaSettings.DefaultRoomBufferMinutes);
        builder
            .Property(x => x.MaxSeatsPerBooking)
            .HasDefaultValue(CinemaSettings.DefaultMaxSeatsPerBooking);
        builder
            .Property(x => x.MaxComboQuantity)
            .HasDefaultValue(CinemaSettings.DefaultMaxComboQuantity);
        builder
            .Property(x => x.LoyaltyWindowMonths)
            .HasDefaultValue(CinemaSettings.DefaultLoyaltyWindowMonths);
        builder
            .Property(x => x.MovieEndGraceDays)
            .HasDefaultValue(CinemaSettings.DefaultMovieEndGraceDays);
        builder
            .Property(x => x.ShowtimeLookaheadDays)
            .HasDefaultValue(CinemaSettings.DefaultShowtimeLookaheadDays);
        builder
            .Property(x => x.SchedulerStepMinutes)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerStepMinutes);
        builder
            .Property(x => x.SchedulerMinSameMovieGapMinutes)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerMinSameMovieGapMinutes);
        builder
            .Property(x => x.SchedulerHeatLookbackDays)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerHeatLookbackDays);
        builder
            .Property(x => x.SchedulerHeatSalesWeight)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerHeatSalesWeight);
        builder
            .Property(x => x.SchedulerHeatRatingWeight)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerHeatRatingWeight);
        builder
            .Property(x => x.SchedulerMaxRating)
            .HasDefaultValue(CinemaSettings.DefaultSchedulerMaxRating);
    }
}
