using Cinema.Application.Caching;
using Cinema.Application.DTOs.Settings;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Settings;
using Cinema.Application.Options;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Settings;

public class SettingsService(
    IAppDbContext db,
    IOptions<BrandOptions> brandOptions,
    IAppCache cache,
    IAuditLogger auditLogger
) : ISettingsService
{
    private readonly BrandOptions _brand = brandOptions.Value;

    public async Task<CinemaSettingsDto> GetSettingsAsync(CancellationToken cancellationToken) =>
        ToDto(await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken));

    public async Task<AdminCinemaSettingsDto> GetAdminSettingsAsync(
        CancellationToken cancellationToken
    )
    {
        var settings =
            await db.CinemaSettings.FirstOrDefaultAsync(
                s => s.Id == AppDbContextExtensions.CinemaSettingsId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_CinemaSettings);

        return ToAdminDto(settings);
    }

    public async Task<AdminCinemaSettingsDto> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken
    )
    {
        var settings =
            await db.CinemaSettings.FirstOrDefaultAsync(
                s => s.Id == AppDbContextExtensions.CinemaSettingsId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_CinemaSettings);

        var before = AuditSnapshots.Settings(settings, false);

        if (request.CancelRefundPercent is < 0 or > 100)
            throw new BusinessRuleException(Messages.Rule_CancelRefundPercentRange);
        if (request.CancelDeadlineHours < 0)
            throw new BusinessRuleException(Messages.Rule_CancelDeadlineHoursNegative);

        settings.Name = request.Name;
        settings.Address = request.Address;
        settings.Phone = request.Phone;
        settings.Email = request.Email;
        settings.TaxCode = request.TaxCode;
        settings.LogoUrl = request.LogoUrl;
        settings.OpeningTime = DateTimeUtils.ParseTimeOnly(request.OpeningTime);
        settings.ClosingTime = DateTimeUtils.ParseTimeOnly(request.ClosingTime);
        settings.PointsPerAmount = request.PointsPerAmount;
        settings.CancelRefundPercent = request.CancelRefundPercent;
        settings.CancelDeadlineHours = request.CancelDeadlineHours;
        settings.StopSellingMinutesBefore = request.StopSellingMinutesBefore;
        if (request.HoldDurationMinutes is { } holdDurationMinutes)
            settings.HoldDurationMinutes = holdDurationMinutes;
        if (request.CheckInOpenMinutes is { } checkInOpenMinutes)
            settings.CheckInOpenMinutes = checkInOpenMinutes;
        if (Enum.TryParse<SeatGapRuleMode>(request.SeatGapRuleMode, out var seatGapRuleMode))
            settings.SeatGapRuleMode = seatGapRuleMode;
        if (request.RoomBufferMinutes is { } roomBufferMinutes)
            settings.RoomBufferMinutes = roomBufferMinutes;
        if (request.MaxSeatsPerBooking is { } maxSeatsPerBooking)
            settings.MaxSeatsPerBooking = maxSeatsPerBooking;
        if (request.MaxComboQuantity is { } maxComboQuantity)
            settings.MaxComboQuantity = maxComboQuantity;
        if (request.LoyaltyWindowMonths is { } loyaltyWindowMonths)
            settings.LoyaltyWindowMonths = loyaltyWindowMonths;
        if (request.PointsExpiryMonths is { } pointsExpiryMonths)
            settings.PointsExpiryMonths = pointsExpiryMonths;
        if (request.MovieEndGraceDays is { } movieEndGraceDays)
            settings.MovieEndGraceDays = movieEndGraceDays;
        if (request.ShowtimeLookaheadDays is { } showtimeLookaheadDays)
            settings.ShowtimeLookaheadDays = showtimeLookaheadDays;
        if (request.SchedulerStepMinutes is { } schedulerStepMinutes)
            settings.SchedulerStepMinutes = schedulerStepMinutes;
        if (request.SchedulerMinSameMovieGapMinutes is { } schedulerMinGap)
            settings.SchedulerMinSameMovieGapMinutes = schedulerMinGap;
        if (request.SchedulerHeatLookbackDays is { } schedulerLookback)
            settings.SchedulerHeatLookbackDays = schedulerLookback;
        if (request.SchedulerHeatSalesWeight is { } salesWeight)
            settings.SchedulerHeatSalesWeight = salesWeight;
        if (request.SchedulerHeatRatingWeight is { } ratingWeight)
            settings.SchedulerHeatRatingWeight = ratingWeight;
        if (request.SchedulerMaxRating is { } schedulerMaxRating)
            settings.SchedulerMaxRating = schedulerMaxRating;
        if (request.TicketPriceRoundingStep is { } ticketPriceRoundingStep)
            settings.TicketPriceRoundingStep = ticketPriceRoundingStep;
        if (request.DynamicPricingMaxIncreasePercent is { } maxIncreasePercent)
            settings.DynamicPricingMaxIncreasePercent = maxIncreasePercent;
        if (request.DynamicPricingMaxDecreasePercent is { } maxDecreasePercent)
            settings.DynamicPricingMaxDecreasePercent = maxDecreasePercent;
        if (request.MinTicketPrice is { } minTicketPrice)
            settings.MinTicketPrice = minTicketPrice;
        if (request.SalesPaceCustomMarkMinutes is { } salesPaceCustomMarkMinutes)
            settings.SalesPaceCustomMarkMinutes = salesPaceCustomMarkMinutes;
        if (request.SuggestionHighOccupancyPercent is { } suggestionHigh)
            settings.SuggestionHighOccupancyPercent = suggestionHigh;
        if (request.SuggestionLowOccupancyPercent is { } suggestionLow)
            settings.SuggestionLowOccupancyPercent = suggestionLow;
        if (request.SuggestionMinShowtimes is { } suggestionMin)
            settings.SuggestionMinShowtimes = suggestionMin;
        if (request.SuggestionLookbackDays is { } suggestionLookback)
            settings.SuggestionLookbackDays = suggestionLookback;
        if (request.DailyClosingAllowOverride is { } dailyClosingAllowOverride)
            settings.DailyClosingAllowOverride = dailyClosingAllowOverride;
        if (request.TicketVatPercent is { } ticketVatPercent)
            settings.TicketVatPercent = ticketVatPercent;
        if (request.ComboVatPercent is { } comboVatPercent)
            settings.ComboVatPercent = comboVatPercent;
        if (request.GiftCardVatPercent is { } giftCardVatPercent)
            settings.GiftCardVatPercent = giftCardVatPercent;
        if (request.MembershipVatPercent is { } membershipVatPercent)
            settings.MembershipVatPercent = membershipVatPercent;
        if (request.InvoiceRequestWindowDays is { } invoiceRequestWindowDays)
            settings.InvoiceRequestWindowDays = invoiceRequestWindowDays;
        if (request.GiftCardDenominations is { } giftCardDenominations)
            settings.GiftCardDenominations = [.. giftCardDenominations.Order()];
        if (request.GiftCardMinAmount is { } giftCardMinAmount)
            settings.GiftCardMinAmount = giftCardMinAmount;
        if (request.GiftCardMaxAmount is { } giftCardMaxAmount)
            settings.GiftCardMaxAmount = giftCardMaxAmount;
        if (request.GiftCardValidityMonths is { } giftCardValidityMonths)
            settings.GiftCardValidityMonths = giftCardValidityMonths;
        if (request.GiftCardRefundExtensionDays is { } giftCardRefundExtensionDays)
            settings.GiftCardRefundExtensionDays = giftCardRefundExtensionDays;
        if (request.GiftCardMaxFailedAttempts is { } giftCardMaxFailedAttempts)
            settings.GiftCardMaxFailedAttempts = giftCardMaxFailedAttempts;
        if (request.GiftCardLockoutMinutes is { } giftCardLockoutMinutes)
            settings.GiftCardLockoutMinutes = giftCardLockoutMinutes;
        if (request.ExchangeDeadlineHours is { } exchangeDeadlineHours)
            settings.ExchangeDeadlineHours = exchangeDeadlineHours;
        if (request.MaxExchangesPerOrder is { } maxExchangesPerOrder)
            settings.MaxExchangesPerOrder = maxExchangesPerOrder;
        if (request.ExchangeCreditValidityDays is { } exchangeCreditValidityDays)
            settings.ExchangeCreditValidityDays = exchangeCreditValidityDays;
        if (request.PreorderComboDiscountPercent is { } preorderComboDiscountPercent)
            settings.PreorderComboDiscountPercent = preorderComboDiscountPercent;
        if (request.ReminderHoursBefore is { } reminderHoursBefore)
            settings.ReminderHoursBefore = reminderHoursBefore;
        if (request.PointsExpiryReminderDays is { } pointsExpiryReminderDays)
            settings.PointsExpiryReminderDays = pointsExpiryReminderDays;
        if (settings.GiftCardMinAmount > settings.GiftCardMaxAmount)
            throw new BusinessRuleException(Messages.Validation_GiftCardAmount_MinGreaterThanMax);
        if (
            settings.GiftCardDenominations.Any(d =>
                d < settings.GiftCardMinAmount || d > settings.GiftCardMaxAmount
            )
        )
            throw new BusinessRuleException(Messages.Validation_GiftCardDenominations_OutOfRange);
        if (settings.SchedulerHeatSalesWeight + settings.SchedulerHeatRatingWeight <= 0)
            throw new BusinessRuleException(Messages.Validation_SchedulerHeatWeights_SumPositive);
        if (settings.SuggestionHighOccupancyPercent <= settings.SuggestionLowOccupancyPercent)
            throw new BusinessRuleException(
                Messages.Validation_SuggestionOccupancy_HighGreaterThanLow
            );
        var passwordChanged = false;
        if (request.DefaultEmployeePassword is { } defaultEmployeePassword)
        {
            passwordChanged = defaultEmployeePassword != settings.DefaultEmployeePassword;
            settings.DefaultEmployeePassword = defaultEmployeePassword;
        }
        settings.UpdatedAt = DateTime.UtcNow;

        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.CinemaSettings,
            settings.Id.ToString(),
            before,
            AuditSnapshots.Settings(settings, passwordChanged)
        );
        await db.SaveChangesAsync(cancellationToken);
        await cache.RefreshAsync(CacheKeys.CinemaSettings, cancellationToken);

        return ToAdminDto(settings);
    }

    private AdminCinemaSettingsDto ToAdminDto(CinemaSettings settings) =>
        new(ToDto(settings), settings.DefaultEmployeePassword)
        {
            PointsExpiryMonths = settings.PointsExpiryMonths,
            TicketPriceRoundingStep = settings.TicketPriceRoundingStep,
            DynamicPricingMaxIncreasePercent = settings.DynamicPricingMaxIncreasePercent,
            DynamicPricingMaxDecreasePercent = settings.DynamicPricingMaxDecreasePercent,
            MinTicketPrice = settings.MinTicketPrice,
            SalesPaceCustomMarkMinutes = settings.SalesPaceCustomMarkMinutes,
            SuggestionHighOccupancyPercent = settings.SuggestionHighOccupancyPercent,
            SuggestionLowOccupancyPercent = settings.SuggestionLowOccupancyPercent,
            SuggestionMinShowtimes = settings.SuggestionMinShowtimes,
            SuggestionLookbackDays = settings.SuggestionLookbackDays,
            DailyClosingAllowOverride = settings.DailyClosingAllowOverride,
            TicketVatPercent = settings.TicketVatPercent,
            ComboVatPercent = settings.ComboVatPercent,
            GiftCardVatPercent = settings.GiftCardVatPercent,
            MembershipVatPercent = settings.MembershipVatPercent,
            InvoiceRequestWindowDays = settings.InvoiceRequestWindowDays,
            GiftCardDenominations = [.. settings.GiftCardDenominations],
            GiftCardMinAmount = settings.GiftCardMinAmount,
            GiftCardMaxAmount = settings.GiftCardMaxAmount,
            GiftCardValidityMonths = settings.GiftCardValidityMonths,
            GiftCardRefundExtensionDays = settings.GiftCardRefundExtensionDays,
            GiftCardMaxFailedAttempts = settings.GiftCardMaxFailedAttempts,
            GiftCardLockoutMinutes = settings.GiftCardLockoutMinutes,
            ExchangeDeadlineHours = settings.ExchangeDeadlineHours,
            MaxExchangesPerOrder = settings.MaxExchangesPerOrder,
            ExchangeCreditValidityDays = settings.ExchangeCreditValidityDays,
            PreorderComboDiscountPercent = settings.PreorderComboDiscountPercent,
            ReminderHoursBefore = settings.ReminderHoursBefore,
            PointsExpiryReminderDays = settings.PointsExpiryReminderDays,
        };

    private CinemaSettingsDto ToDto(object settings) =>
        settings.Adapt<CinemaSettingsDto>() with
        {
            TimeZoneId = BusinessTime.IanaId,
            Locale = _brand.Locale,
            CurrencyCode = _brand.CurrencyCode,
            CurrencySymbol = _brand.CurrencySymbol,
        };
}
