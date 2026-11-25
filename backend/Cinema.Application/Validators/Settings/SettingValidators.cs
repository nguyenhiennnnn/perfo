using System.Globalization;
using Cinema.Application.DTOs.Settings;
using Cinema.Application.Validators.Auth;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Settings;

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(Messages.Validation_CinemaName_Required)
            .MaximumLength(FieldLengths.CinemaName)
            .WithMessage(Messages.Validation_CinemaName_MaxLength150);

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage(Messages.Validation_Address_Required)
            .MaximumLength(FieldLengths.CinemaAddress)
            .WithMessage(Messages.Validation_Address_MaxLength255);

        RuleFor(x => x.Phone).MaximumLength(FieldLengths.Phone).WithMessage(Messages.Validation_Phone_MaxLength20);

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(Messages.Validation_Email_Invalid)
            .MaximumLength(FieldLengths.Email)
            .WithMessage(Messages.Validation_Email_MaxLength150)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.TaxCode)
            .MaximumLength(FieldLengths.Name)
            .WithMessage(Messages.Validation_TaxCode_MaxLength50);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(FieldLengths.Url)
            .WithMessage(Messages.Validation_LogoUrl_MaxLength255);

        RuleFor(x => x.OpeningTime)
            .Must(BeValidTime)
            .When(x => x.OpeningTime is not null)
            .WithMessage(Messages.Validation_OpeningTime_TimeFormat);

        RuleFor(x => x.ClosingTime)
            .Must(BeValidTime)
            .When(x => x.ClosingTime is not null)
            .WithMessage(Messages.Validation_ClosingTime_TimeFormat);

        RuleFor(x => x.PointsPerAmount)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_PointsPerAmount_GreaterThanZero);

        RuleFor(x => x.CancelRefundPercent)
            .InclusiveBetween(0, 100)
            .WithMessage(Messages.Validation_CancelRefundPercent_Range0To100);

        RuleFor(x => x.CancelDeadlineHours)
            .GreaterThanOrEqualTo(0)
            .WithMessage(Messages.Validation_CancelDeadlineHours_NonNegative);

        RuleFor(x => x.StopSellingMinutesBefore)
            .InclusiveBetween(0, 1440)
            .WithMessage(Messages.Validation_StopSellingMinutesBefore_Range0To1440);

        RuleFor(x => x.HoldDurationMinutes)
            .Must(v => v is >= 1 and <= 120)
            .When(x => x.HoldDurationMinutes is not null)
            .WithMessage(Messages.Validation_HoldDurationMinutes_Range1To120);

        RuleFor(x => x.CheckInOpenMinutes)
            .Must(v => v is >= 0 and <= 240)
            .When(x => x.CheckInOpenMinutes is not null)
            .WithMessage(Messages.Validation_CheckInOpenMinutes_Range0To240);

        RuleFor(x => x.SeatGapRuleMode)
            .Must(v => Enum.TryParse<SeatGapRuleMode>(v, out _) && !int.TryParse(v, out _))
            .When(x => x.SeatGapRuleMode is not null)
            .WithMessage(Messages.Validation_SeatGapRuleMode_Invalid);

        RuleFor(x => x.DefaultEmployeePassword!)
            .StrongPassword()
            .MaximumLength(FieldLengths.DefaultEmployeePassword)
            .WithMessage(Messages.Validation_DefaultEmployeePassword_MaxLength100)
            .When(x => x.DefaultEmployeePassword is not null);

        IntRange(x => x.RoomBufferMinutes, "roomBufferMinutes", 0, 120);
        IntRange(x => x.MaxSeatsPerBooking, "maxSeatsPerBooking", 1, 50);
        IntRange(x => x.MaxComboQuantity, "maxComboQuantity", 1, 50);
        IntRange(x => x.LoyaltyWindowMonths, "loyaltyWindowMonths", 1, 120);
        IntRange(x => x.PointsExpiryMonths, "pointsExpiryMonths", 1, 120);
        IntRange(x => x.MovieEndGraceDays, "movieEndGraceDays", 0, 365);
        IntRange(x => x.ShowtimeLookaheadDays, "showtimeLookaheadDays", 1, 60);
        IntRange(x => x.SchedulerStepMinutes, "schedulerStepMinutes", 1, 60);
        IntRange(
            x => x.SchedulerMinSameMovieGapMinutes,
            "schedulerMinSameMovieGapMinutes",
            0,
            1440
        );
        IntRange(x => x.SchedulerHeatLookbackDays, "schedulerHeatLookbackDays", 1, 365);
        DoubleRange(x => x.SchedulerHeatSalesWeight, "schedulerHeatSalesWeight", 0, 10);
        DoubleRange(x => x.SchedulerHeatRatingWeight, "schedulerHeatRatingWeight", 0, 10);
        DoubleRange(x => x.SchedulerMaxRating, "schedulerMaxRating", 1, 10);
        WholeDecimalRange(x => x.TicketPriceRoundingStep, "ticketPriceRoundingStep", 1, 100_000);
        DecimalRange(
            x => x.DynamicPricingMaxIncreasePercent,
            "dynamicPricingMaxIncreasePercent",
            0,
            300
        );
        DecimalRange(
            x => x.DynamicPricingMaxDecreasePercent,
            "dynamicPricingMaxDecreasePercent",
            0,
            100
        );
        WholeDecimalRange(x => x.MinTicketPrice, "minTicketPrice", 0, 10_000_000);
        IntRange(x => x.SalesPaceCustomMarkMinutes, "salesPaceCustomMarkMinutes", 0, 10080);
        IntRange(x => x.SuggestionHighOccupancyPercent, "suggestionHighOccupancyPercent", 0, 100);
        IntRange(x => x.SuggestionLowOccupancyPercent, "suggestionLowOccupancyPercent", 0, 100);
        IntRange(x => x.SuggestionMinShowtimes, "suggestionMinShowtimes", 1, 1000);
        IntRange(x => x.SuggestionLookbackDays, "suggestionLookbackDays", 1, 365);
        DecimalRange(x => x.TicketVatPercent, "ticketVatPercent", 0, 30);
        DecimalRange(x => x.ComboVatPercent, "comboVatPercent", 0, 30);
        DecimalRange(x => x.GiftCardVatPercent, "giftCardVatPercent", 0, 30);
        DecimalRange(x => x.MembershipVatPercent, "membershipVatPercent", 0, 30);
        IntRange(x => x.InvoiceRequestWindowDays, "invoiceRequestWindowDays", 1, 365);
        WholeDecimalRange(x => x.GiftCardMinAmount, "giftCardMinAmount", 1_000, 100_000_000);
        WholeDecimalRange(x => x.GiftCardMaxAmount, "giftCardMaxAmount", 1_000, 100_000_000);
        IntRange(x => x.GiftCardValidityMonths, "giftCardValidityMonths", 1, 60);
        IntRange(x => x.GiftCardRefundExtensionDays, "giftCardRefundExtensionDays", 1, 365);
        IntRange(x => x.GiftCardMaxFailedAttempts, "giftCardMaxFailedAttempts", 3, 20);
        IntRange(x => x.GiftCardLockoutMinutes, "giftCardLockoutMinutes", 1, 1440);
        IntRange(x => x.ExchangeDeadlineHours, "exchangeDeadlineHours", 0, 720);
        IntRange(x => x.MaxExchangesPerOrder, "maxExchangesPerOrder", 1, 10);
        IntRange(x => x.ExchangeCreditValidityDays, "exchangeCreditValidityDays", 1, 3650);
        IntRange(x => x.PreorderComboDiscountPercent, "preorderComboDiscountPercent", 0, 100);
        IntRange(x => x.ReminderHoursBefore, "reminderHoursBefore", 1, 72);
        IntRange(x => x.PointsExpiryReminderDays, "pointsExpiryReminderDays", 1, 90);
        IntRange(x => x.ReferralReferrerPoints, "referralReferrerPoints", 0, 100000);
        IntRange(x => x.ReferralReferredPoints, "referralReferredPoints", 0, 100000);
        IntRange(x => x.ReferralMonthlyRewardLimit, "referralMonthlyRewardLimit", 1, 1000);
        IntRange(x => x.SupportRequestDueHours, "supportRequestDueHours", 1, 720);
        IntRange(x => x.ReviewInviteMinHours, "reviewInviteMinHours", 0, 72);
        IntRange(x => x.ReviewInviteMaxHours, "reviewInviteMaxHours", 1, 168);
        IntRange(x => x.ReviewRewardPoints, "reviewRewardPoints", 0, 100000);
        DecimalRange(x => x.RoomQualityAlertThreshold, "roomQualityAlertThreshold", 1, 5);
        IntRange(x => x.RoomQualityAlertDays, "roomQualityAlertDays", 1, 60);
        WholeDecimalRange(
            x => x.ExceptionalRefundApprovalThreshold,
            "exceptionalRefundApprovalThreshold",
            0,
            100_000_000
        );

        RuleFor(x => x.GiftCardDenominations!)
            .Must(BeValidDenominations)
            .When(x => x.GiftCardDenominations is not null)
            .WithMessage(Messages.Validation_GiftCardDenominations_Invalid);

        RuleFor(x => x)
            .Must(x => x.GiftCardMinAmount!.Value <= x.GiftCardMaxAmount!.Value)
            .When(x => x.GiftCardMinAmount is not null && x.GiftCardMaxAmount is not null)
            .WithName("giftCardMinAmount")
            .WithMessage(Messages.Validation_GiftCardAmount_MinGreaterThanMax);

        RuleFor(x => x)
            .Must(x => x.SuggestionHighOccupancyPercent!.Value > x.SuggestionLowOccupancyPercent!.Value)
            .When(x =>
                x.SuggestionHighOccupancyPercent is not null
                && x.SuggestionLowOccupancyPercent is not null
            )
            .WithName("suggestionHighOccupancyPercent")
            .WithMessage(Messages.Validation_SuggestionOccupancy_HighGreaterThanLow);

        RuleFor(x => x)
            .Must(x => x.SchedulerHeatSalesWeight!.Value + x.SchedulerHeatRatingWeight!.Value > 0)
            .When(x =>
                x.SchedulerHeatSalesWeight is not null && x.SchedulerHeatRatingWeight is not null
            )
            .WithName("schedulerHeatSalesWeight")
            .WithMessage(Messages.Validation_SchedulerHeatWeights_SumPositive);
    }

    private void IntRange(
        System.Linq.Expressions.Expression<Func<UpdateSettingsRequest, int?>> selector,
        string name,
        int min,
        int max
    )
    {
        var get = selector.Compile();
        RuleFor(selector)
            .Must(v => v is not null && v >= min && v <= max)
            .When(x => get(x) is not null)
            .WithMessage(string.Format(Messages.Validation_Setting_NumberRange, name, min, max));
    }

    private void DoubleRange(
        System.Linq.Expressions.Expression<Func<UpdateSettingsRequest, double?>> selector,
        string name,
        double min,
        double max
    )
    {
        var get = selector.Compile();
        RuleFor(selector)
            .Must(v => v is not null && !double.IsNaN(v.Value) && v >= min && v <= max)
            .When(x => get(x) is not null)
            .WithMessage(string.Format(Messages.Validation_Setting_NumberRange, name, min, max));
    }

    private void DecimalRange(
        System.Linq.Expressions.Expression<Func<UpdateSettingsRequest, decimal?>> selector,
        string name,
        decimal min,
        decimal max
    )
    {
        var get = selector.Compile();
        RuleFor(selector)
            .Must(v => v is not null && v >= min && v <= max)
            .When(x => get(x) is not null)
            .WithMessage(string.Format(Messages.Validation_Setting_NumberRange, name, min, max));
    }

    private void WholeDecimalRange(
        System.Linq.Expressions.Expression<Func<UpdateSettingsRequest, decimal?>> selector,
        string name,
        decimal min,
        decimal max
    )
    {
        var get = selector.Compile();
        RuleFor(selector)
            .Must(v => v is not null && v >= min && v <= max && v == decimal.Truncate(v.Value))
            .When(x => get(x) is not null)
            .WithMessage(string.Format(Messages.Validation_Setting_NumberRange, name, min, max));
    }

    private static bool BeValidDenominations(decimal[] values) =>
        values.Length is >= 1 and <= 20
        && values.All(v => v > 0 && v == decimal.Truncate(v))
        && values.Distinct().Count() == values.Length;

    private static bool BeValidTime(string? value) =>
        value is not null
        && TimeOnly.TryParseExact(
            value,
            DateTimePatterns.HourMinute,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );
}
