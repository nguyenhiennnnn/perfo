namespace Cinema.Application.DTOs.Settings;

public record CinemaSettingsDto(
    string Name,
    string Address,
    string? Phone,
    string? Email,
    string? TaxCode,
    string? LogoUrl,
    string? OpeningTime,
    string? ClosingTime,
    decimal PointsPerAmount,
    decimal CancelRefundPercent,
    int CancelDeadlineHours,
    int StopSellingMinutesBefore,
    int HoldDurationMinutes,
    int CheckInOpenMinutes,
    string SeatGapRuleMode,
    int RoomBufferMinutes,
    int MaxSeatsPerBooking,
    int MaxComboQuantity,
    int LoyaltyWindowMonths,
    int MovieEndGraceDays,
    int ShowtimeLookaheadDays,
    int SchedulerStepMinutes,
    int SchedulerMinSameMovieGapMinutes,
    int SchedulerHeatLookbackDays,
    double SchedulerHeatSalesWeight,
    double SchedulerHeatRatingWeight,
    double SchedulerMaxRating,
    decimal[] GiftCardDenominations,
    decimal GiftCardMinAmount,
    decimal GiftCardMaxAmount,
    int GiftCardValidityMonths
)
{
    public string TimeZoneId { get; init; } = string.Empty;

    public string Locale { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public string CurrencySymbol { get; init; } = string.Empty;
}

/// <summary>
/// Chỉ trả qua endpoint admin. TUYỆT ĐỐI không dùng cho endpoint công khai
/// (GET /api/v1/settings) vì chứa mật khẩu mặc định của nhân viên.
/// </summary>
public record AdminCinemaSettingsDto : CinemaSettingsDto
{
    public AdminCinemaSettingsDto(CinemaSettingsDto source, string defaultEmployeePassword)
        : base(source)
    {
        DefaultEmployeePassword = defaultEmployeePassword;
    }

    public string DefaultEmployeePassword { get; init; }

    public int PointsExpiryMonths { get; init; }

    public decimal TicketPriceRoundingStep { get; init; }

    public decimal DynamicPricingMaxIncreasePercent { get; init; }

    public decimal DynamicPricingMaxDecreasePercent { get; init; }

    public decimal MinTicketPrice { get; init; }

    public int SalesPaceCustomMarkMinutes { get; init; }

    public int SuggestionHighOccupancyPercent { get; init; }

    public int SuggestionLowOccupancyPercent { get; init; }

    public int SuggestionMinShowtimes { get; init; }

    public int SuggestionLookbackDays { get; init; }

    public bool DailyClosingAllowOverride { get; init; }

    public decimal TicketVatPercent { get; init; }

    public decimal ComboVatPercent { get; init; }

    public decimal GiftCardVatPercent { get; init; }

    public decimal MembershipVatPercent { get; init; }

    public int InvoiceRequestWindowDays { get; init; }

    public int GiftCardRefundExtensionDays { get; init; }

    public int GiftCardMaxFailedAttempts { get; init; }

    public int GiftCardLockoutMinutes { get; init; }

    public int ExchangeDeadlineHours { get; init; }

    public int MaxExchangesPerOrder { get; init; }

    public int ExchangeCreditValidityDays { get; init; }

    public int PreorderComboDiscountPercent { get; init; }

    public int ReminderHoursBefore { get; init; }

    public int PointsExpiryReminderDays { get; init; }

    public int ReferralReferrerPoints { get; init; }

    public int ReferralReferredPoints { get; init; }

    public int ReferralMonthlyRewardLimit { get; init; }

    public int SupportRequestDueHours { get; init; }

    public decimal ExceptionalRefundApprovalThreshold { get; init; }

    public int ReviewInviteMinHours { get; init; }

    public int ReviewInviteMaxHours { get; init; }

    public int ReviewRewardPoints { get; init; }

    public decimal RoomQualityAlertThreshold { get; init; }

    public int RoomQualityAlertDays { get; init; }

    public int WaitlistPriorityMinutes { get; init; }

    public int ExpiredOrderStreakThreshold { get; init; }

    public int ExpiredOrderRestrictionMinutes { get; init; }

    public int RestrictedHoldMinutes { get; init; }

    public int RestrictedMaxPendingOrders { get; init; }

    public int MaxPendingOrdersPerUser { get; init; }

    public int MaxHeldSeatsPerUser { get; init; }

    public int PublicFormMaxRequests { get; init; }

    public int PublicFormWindowMinutes { get; init; }

    public int AbuseHoldAbandonThreshold { get; init; }

    public int AbuseHoldAbandonWindowMinutes { get; init; }

    public int AbusePaymentFailureThreshold { get; init; }

    public int AbusePaymentFailureWindowMinutes { get; init; }

    public int VerificationCodeMinutes { get; init; }

    public int VerificationMaxAttempts { get; init; }
}

public record UpdateSettingsRequest(
    string Name,
    string Address,
    string? Phone,
    string? Email,
    string? TaxCode,
    string? LogoUrl,
    string? OpeningTime,
    string? ClosingTime,
    decimal PointsPerAmount,
    decimal CancelRefundPercent,
    int CancelDeadlineHours,
    int StopSellingMinutesBefore = 0,
    int? HoldDurationMinutes = null,
    int? CheckInOpenMinutes = null,
    string? SeatGapRuleMode = null,
    int? RoomBufferMinutes = null,
    int? MaxSeatsPerBooking = null,
    int? MaxComboQuantity = null,
    int? LoyaltyWindowMonths = null,
    int? PointsExpiryMonths = null,
    int? MovieEndGraceDays = null,
    int? ShowtimeLookaheadDays = null,
    int? SchedulerStepMinutes = null,
    int? SchedulerMinSameMovieGapMinutes = null,
    int? SchedulerHeatLookbackDays = null,
    double? SchedulerHeatSalesWeight = null,
    double? SchedulerHeatRatingWeight = null,
    double? SchedulerMaxRating = null,
    string? DefaultEmployeePassword = null,
    decimal? TicketPriceRoundingStep = null,
    decimal? DynamicPricingMaxIncreasePercent = null,
    decimal? DynamicPricingMaxDecreasePercent = null,
    decimal? MinTicketPrice = null,
    int? SalesPaceCustomMarkMinutes = null,
    int? SuggestionHighOccupancyPercent = null,
    int? SuggestionLowOccupancyPercent = null,
    int? SuggestionMinShowtimes = null,
    int? SuggestionLookbackDays = null,
    bool? DailyClosingAllowOverride = null,
    decimal? TicketVatPercent = null,
    decimal? ComboVatPercent = null,
    decimal? GiftCardVatPercent = null,
    decimal? MembershipVatPercent = null,
    int? InvoiceRequestWindowDays = null,
    decimal[]? GiftCardDenominations = null,
    decimal? GiftCardMinAmount = null,
    decimal? GiftCardMaxAmount = null,
    int? GiftCardValidityMonths = null,
    int? GiftCardRefundExtensionDays = null,
    int? GiftCardMaxFailedAttempts = null,
    int? GiftCardLockoutMinutes = null,
    int? ExchangeDeadlineHours = null,
    int? MaxExchangesPerOrder = null,
    int? ExchangeCreditValidityDays = null,
    int? PreorderComboDiscountPercent = null,
    int? ReminderHoursBefore = null,
    int? PointsExpiryReminderDays = null,
    int? ReferralReferrerPoints = null,
    int? ReferralReferredPoints = null,
    int? ReferralMonthlyRewardLimit = null,
    int? SupportRequestDueHours = null,
    decimal? ExceptionalRefundApprovalThreshold = null,
    int? ReviewInviteMinHours = null,
    int? ReviewInviteMaxHours = null,
    int? ReviewRewardPoints = null,
    decimal? RoomQualityAlertThreshold = null,
    int? RoomQualityAlertDays = null,
    int? WaitlistPriorityMinutes = null,
    int? ExpiredOrderStreakThreshold = null,
    int? ExpiredOrderRestrictionMinutes = null,
    int? RestrictedHoldMinutes = null,
    int? RestrictedMaxPendingOrders = null,
    int? MaxPendingOrdersPerUser = null,
    int? MaxHeldSeatsPerUser = null,
    int? PublicFormMaxRequests = null,
    int? PublicFormWindowMinutes = null,
    int? AbuseHoldAbandonThreshold = null,
    int? AbuseHoldAbandonWindowMinutes = null,
    int? AbusePaymentFailureThreshold = null,
    int? AbusePaymentFailureWindowMinutes = null,
    int? VerificationCodeMinutes = null,
    int? VerificationMaxAttempts = null
);
