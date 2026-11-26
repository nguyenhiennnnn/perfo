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
            .Property(x => x.PointsExpiryMonths)
            .HasDefaultValue(CinemaSettings.DefaultPointsExpiryMonths);
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
        builder
            .Property(x => x.TicketPriceRoundingStep)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(CinemaSettings.DefaultTicketPriceRoundingStep);
        builder
            .Property(x => x.DynamicPricingMaxIncreasePercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultDynamicPricingMaxIncreasePercent);
        builder
            .Property(x => x.DynamicPricingMaxDecreasePercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultDynamicPricingMaxDecreasePercent);
        builder
            .Property(x => x.MinTicketPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(CinemaSettings.DefaultMinTicketPrice);
        builder
            .Property(x => x.SalesPaceCustomMarkMinutes)
            .HasDefaultValue(CinemaSettings.DefaultSalesPaceCustomMarkMinutes);
        builder
            .Property(x => x.SuggestionHighOccupancyPercent)
            .HasDefaultValue(CinemaSettings.DefaultSuggestionHighOccupancyPercent);
        builder
            .Property(x => x.SuggestionLowOccupancyPercent)
            .HasDefaultValue(CinemaSettings.DefaultSuggestionLowOccupancyPercent);
        builder
            .Property(x => x.SuggestionMinShowtimes)
            .HasDefaultValue(CinemaSettings.DefaultSuggestionMinShowtimes);
        builder
            .Property(x => x.SuggestionLookbackDays)
            .HasDefaultValue(CinemaSettings.DefaultSuggestionLookbackDays);
        builder.Property(x => x.DailyClosingAllowOverride).HasDefaultValue(false);
        builder
            .Property(x => x.TicketVatPercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultVatPercent);
        builder
            .Property(x => x.ComboVatPercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultVatPercent);
        builder
            .Property(x => x.GiftCardVatPercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultVatPercent);
        builder
            .Property(x => x.MembershipVatPercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(CinemaSettings.DefaultVatPercent);
        builder
            .Property(x => x.InvoiceRequestWindowDays)
            .HasDefaultValue(CinemaSettings.DefaultInvoiceRequestWindowDays);
        builder
            .Property(x => x.GiftCardDenominations)
            .HasColumnType("numeric(18,2)[]")
            .HasDefaultValueSql(
                "ARRAY["
                    + string.Join(
                        ",",
                        CinemaSettings.DefaultGiftCardDenominations.Select(v =>
                            v.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        )
                    )
                    + "]::numeric(18,2)[]"
            );
        builder
            .Property(x => x.GiftCardMinAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(CinemaSettings.DefaultGiftCardMinAmount);
        builder
            .Property(x => x.GiftCardMaxAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(CinemaSettings.DefaultGiftCardMaxAmount);
        builder
            .Property(x => x.GiftCardValidityMonths)
            .HasDefaultValue(CinemaSettings.DefaultGiftCardValidityMonths);
        builder
            .Property(x => x.GiftCardRefundExtensionDays)
            .HasDefaultValue(CinemaSettings.DefaultGiftCardRefundExtensionDays);
        builder
            .Property(x => x.GiftCardMaxFailedAttempts)
            .HasDefaultValue(CinemaSettings.DefaultGiftCardMaxFailedAttempts);
        builder
            .Property(x => x.GiftCardLockoutMinutes)
            .HasDefaultValue(CinemaSettings.DefaultGiftCardLockoutMinutes);
        builder
            .Property(x => x.ExchangeDeadlineHours)
            .HasDefaultValue(CinemaSettings.DefaultExchangeDeadlineHours);
        builder
            .Property(x => x.MaxExchangesPerOrder)
            .HasDefaultValue(CinemaSettings.DefaultMaxExchangesPerOrder);
        builder
            .Property(x => x.ExchangeCreditValidityDays)
            .HasDefaultValue(CinemaSettings.DefaultExchangeCreditValidityDays);
        builder
            .Property(x => x.PreorderComboDiscountPercent)
            .HasDefaultValue(CinemaSettings.DefaultPreorderComboDiscountPercent);
        builder
            .Property(x => x.ReminderHoursBefore)
            .HasDefaultValue(CinemaSettings.DefaultReminderHoursBefore);
        builder
            .Property(x => x.PointsExpiryReminderDays)
            .HasDefaultValue(CinemaSettings.DefaultPointsExpiryReminderDays);
        builder
            .Property(x => x.ReferralReferrerPoints)
            .HasDefaultValue(CinemaSettings.DefaultReferralReferrerPoints);
        builder
            .Property(x => x.ReferralReferredPoints)
            .HasDefaultValue(CinemaSettings.DefaultReferralReferredPoints);
        builder
            .Property(x => x.ReferralMonthlyRewardLimit)
            .HasDefaultValue(CinemaSettings.DefaultReferralMonthlyRewardLimit);
        builder
            .Property(x => x.ReviewInviteMinHours)
            .HasDefaultValue(CinemaSettings.DefaultReviewInviteMinHours);
        builder
            .Property(x => x.ReviewInviteMaxHours)
            .HasDefaultValue(CinemaSettings.DefaultReviewInviteMaxHours);
        builder
            .Property(x => x.ReviewRewardPoints)
            .HasDefaultValue(CinemaSettings.DefaultReviewRewardPoints);
        builder
            .Property(x => x.RoomQualityAlertThreshold)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(CinemaSettings.DefaultRoomQualityAlertThreshold);
        builder
            .Property(x => x.RoomQualityAlertDays)
            .HasDefaultValue(CinemaSettings.DefaultRoomQualityAlertDays);
        builder
            .Property(x => x.WaitlistPriorityMinutes)
            .HasDefaultValue(CinemaSettings.DefaultWaitlistPriorityMinutes);
        builder
            .Property(x => x.ExpiredOrderStreakThreshold)
            .HasDefaultValue(CinemaSettings.DefaultExpiredOrderStreakThreshold);
        builder
            .Property(x => x.ExpiredOrderRestrictionMinutes)
            .HasDefaultValue(CinemaSettings.DefaultExpiredOrderRestrictionMinutes);
        builder
            .Property(x => x.RestrictedHoldMinutes)
            .HasDefaultValue(CinemaSettings.DefaultRestrictedHoldMinutes);
        builder
            .Property(x => x.RestrictedMaxPendingOrders)
            .HasDefaultValue(CinemaSettings.DefaultRestrictedMaxPendingOrders);
        builder
            .Property(x => x.MaxPendingOrdersPerUser)
            .HasDefaultValue(CinemaSettings.DefaultMaxPendingOrdersPerUser);
        builder
            .Property(x => x.MaxHeldSeatsPerUser)
            .HasDefaultValue(CinemaSettings.DefaultMaxHeldSeatsPerUser);
        builder
            .Property(x => x.PublicFormMaxRequests)
            .HasDefaultValue(CinemaSettings.DefaultPublicFormMaxRequests);
        builder
            .Property(x => x.PublicFormWindowMinutes)
            .HasDefaultValue(CinemaSettings.DefaultPublicFormWindowMinutes);
        builder
            .Property(x => x.AbuseHoldAbandonThreshold)
            .HasDefaultValue(CinemaSettings.DefaultAbuseHoldAbandonThreshold);
        builder
            .Property(x => x.AbuseHoldAbandonWindowMinutes)
            .HasDefaultValue(CinemaSettings.DefaultAbuseHoldAbandonWindowMinutes);
        builder
            .Property(x => x.AbusePaymentFailureThreshold)
            .HasDefaultValue(CinemaSettings.DefaultAbusePaymentFailureThreshold);
        builder
            .Property(x => x.AbusePaymentFailureWindowMinutes)
            .HasDefaultValue(CinemaSettings.DefaultAbusePaymentFailureWindowMinutes);
        builder
            .Property(x => x.VerificationCodeMinutes)
            .HasDefaultValue(CinemaSettings.DefaultVerificationCodeMinutes);
        builder
            .Property(x => x.VerificationMaxAttempts)
            .HasDefaultValue(CinemaSettings.DefaultVerificationMaxAttempts);
        builder
            .Property(x => x.RoomRentalTrackingTokenValidDays)
            .HasDefaultValue(CinemaSettings.DefaultRoomRentalTrackingTokenValidDays);
        builder
            .Property(x => x.RoomRentalTrackingMaxFailedAttempts)
            .HasDefaultValue(CinemaSettings.DefaultRoomRentalTrackingMaxFailedAttempts);
        builder
            .Property(x => x.RoomRentalTrackingLockoutMinutes)
            .HasDefaultValue(CinemaSettings.DefaultRoomRentalTrackingLockoutMinutes);
        builder
            .Property(x => x.SupportRequestDueHours)
            .HasDefaultValue(CinemaSettings.DefaultSupportRequestDueHours);
        builder
            .Property(x => x.ExceptionalRefundApprovalThreshold)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(CinemaSettings.DefaultExceptionalRefundApprovalThreshold);
    }
}
