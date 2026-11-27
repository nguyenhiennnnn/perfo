using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CinemaSettings
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
    public string? LogoUrl { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string DefaultEmployeePassword { get; set; } = DefaultEmployeePasswordValue;

    public decimal PointsPerAmount { get; set; } = 10000m;

    public decimal CancelRefundPercent { get; set; } = 100m;

    public int CancelDeadlineHours { get; set; } = 2;

    public int StopSellingMinutesBefore { get; set; }

    public const string DefaultEmployeePasswordValue = "Employee@123";
    public const int DefaultHoldDurationMinutes = 15;
    public const int DefaultCheckInOpenMinutes = 30;

    public int HoldDurationMinutes { get; set; } = DefaultHoldDurationMinutes;

    public int CheckInOpenMinutes { get; set; } = DefaultCheckInOpenMinutes;

    public SeatGapRuleMode SeatGapRuleMode { get; set; } = SeatGapRuleMode.Strict;

    public const int DefaultRoomBufferMinutes = 15;
    public const int DefaultMaxSeatsPerBooking = 10;
    public const int DefaultMaxComboQuantity = 10;
    public const int DefaultLoyaltyWindowMonths = 12;
    public const int DefaultPointsExpiryMonths = 12;
    public const int DefaultMovieEndGraceDays = 1;
    public const int DefaultShowtimeLookaheadDays = 7;

    public int RoomBufferMinutes { get; set; } = DefaultRoomBufferMinutes;

    public int MaxSeatsPerBooking { get; set; } = DefaultMaxSeatsPerBooking;

    public int MaxComboQuantity { get; set; } = DefaultMaxComboQuantity;

    public int LoyaltyWindowMonths { get; set; } = DefaultLoyaltyWindowMonths;

    public int PointsExpiryMonths { get; set; } = DefaultPointsExpiryMonths;

    public int MovieEndGraceDays { get; set; } = DefaultMovieEndGraceDays;

    public int ShowtimeLookaheadDays { get; set; } = DefaultShowtimeLookaheadDays;

    public const int DefaultSchedulerStepMinutes = 5;
    public const int DefaultSchedulerMinSameMovieGapMinutes = 60;
    public const int DefaultSchedulerHeatLookbackDays = 7;
    public const double DefaultSchedulerHeatSalesWeight = 0.7;
    public const double DefaultSchedulerHeatRatingWeight = 0.3;
    public const double DefaultSchedulerMaxRating = 5;

    public const decimal DefaultTicketPriceRoundingStep = 1000m;
    public const decimal DefaultDynamicPricingMaxIncreasePercent = 30m;
    public const decimal DefaultDynamicPricingMaxDecreasePercent = 30m;
    public const decimal DefaultMinTicketPrice = 45000m;
    public const int DefaultSalesPaceCustomMarkMinutes = 60;
    public const int DefaultSuggestionHighOccupancyPercent = 80;
    public const int DefaultSuggestionLowOccupancyPercent = 35;
    public const int DefaultSuggestionMinShowtimes = 5;
    public const int DefaultSuggestionLookbackDays = 14;
    public const decimal DefaultVatPercent = 10m;
    public const int DefaultInvoiceRequestWindowDays = 30;
    public const decimal DefaultGiftCardMinAmount = 50_000m;
    public const decimal DefaultGiftCardMaxAmount = 5_000_000m;
    public const int DefaultGiftCardValidityMonths = 12;
    public const int DefaultGiftCardRefundExtensionDays = 30;
    public const int DefaultGiftCardMaxFailedAttempts = 5;
    public const int DefaultGiftCardLockoutMinutes = 15;
    public const int DefaultExchangeDeadlineHours = 2;
    public const int DefaultMaxExchangesPerOrder = 1;
    public const int DefaultExchangeCreditValidityDays = 90;
    public const int DefaultPreorderComboDiscountPercent = 0;
    public const int DefaultReminderHoursBefore = 3;
    public const int DefaultPointsExpiryReminderDays = 14;
    public const int DefaultMembershipExpiryReminderDays = 7;
    public const int DefaultReferralReferrerPoints = 50;
    public const int DefaultReferralReferredPoints = 50;
    public const int DefaultReferralMonthlyRewardLimit = 10;
    public const int DefaultSupportRequestDueHours = 48;
    public const decimal DefaultExceptionalRefundApprovalThreshold = 500_000m;
    public const int DefaultReviewInviteMinHours = 1;
    public const int DefaultReviewInviteMaxHours = 3;
    public const int DefaultReviewRewardPoints = 10;
    public const decimal DefaultRoomQualityAlertThreshold = 3m;
    public const int DefaultRoomQualityAlertDays = 3;
    public const int DefaultWaitlistPriorityMinutes = 10;
    public const int DefaultExpiredOrderStreakThreshold = 3;
    public const int DefaultExpiredOrderRestrictionMinutes = 60;
    public const int DefaultRestrictedHoldMinutes = 5;
    public const int DefaultRestrictedMaxPendingOrders = 1;
    public const int DefaultMaxPendingOrdersPerUser = 3;
    public const int DefaultMaxHeldSeatsPerUser = 20;
    public const int DefaultPublicFormMaxRequests = 5;
    public const int DefaultPublicFormWindowMinutes = 10;
    public const int DefaultAbuseHoldAbandonThreshold = 5;
    public const int DefaultAbuseHoldAbandonWindowMinutes = 15;
    public const int DefaultAbusePaymentFailureThreshold = 3;
    public const int DefaultAbusePaymentFailureWindowMinutes = 15;
    public const int DefaultVerificationCodeMinutes = 10;
    public const int DefaultVerificationMaxAttempts = 5;
    public const int DefaultRoomRentalTrackingTokenValidDays = 60;
    public const int DefaultRoomRentalTrackingMaxFailedAttempts = 5;
    public const int DefaultRoomRentalTrackingLockoutMinutes = 15;
    public const int DefaultRoomRentalDepositPercent = 30;
    public const int DefaultRoomRentalQuoteValidDays = 7;
    public const int DefaultRoomRentalBalanceDueDaysBeforeEvent = 3;
    public const int DefaultRoomRentalFullRefundDays = 7;
    public const int DefaultRoomRentalPartialRefundDays = 3;
    public const int DefaultRoomRentalPartialRefundPercent = 50;
    public const int DefaultMembershipFreeTicketsPerShowtime = 2;
    public const bool DefaultMembershipRefundStartedEnabled = false;
    public const int DefaultSegmentNewCustomerDays = 30;
    public const int DefaultSegmentLapsedDays = 60;
    public const int DefaultSegmentRegularMinOrders = 3;
    public const int DefaultSegmentLookbackDays = 180;
    public const int DefaultSegmentHighValueMinScore = 4;
    public const int DefaultSegmentLtvProjectionMonths = 12;
    public const int DefaultSegmentTierUpgradePercent = 20;
    public const int DefaultSegmentPreferenceMinViewings = 3;
    public const int DefaultSegmentPreferenceMinSharePercent = 40;
    public const int DefaultCampaignMinIntervalDays = 7;

    public static readonly decimal[] DefaultGiftCardDenominations =
    [
        100_000m,
        200_000m,
        500_000m,
        1_000_000m,
    ];

    public int SchedulerStepMinutes { get; set; } = DefaultSchedulerStepMinutes;

    public int SchedulerMinSameMovieGapMinutes { get; set; } =
        DefaultSchedulerMinSameMovieGapMinutes;

    public int SchedulerHeatLookbackDays { get; set; } = DefaultSchedulerHeatLookbackDays;

    public double SchedulerHeatSalesWeight { get; set; } = DefaultSchedulerHeatSalesWeight;

    public double SchedulerHeatRatingWeight { get; set; } = DefaultSchedulerHeatRatingWeight;

    public double SchedulerMaxRating { get; set; } = DefaultSchedulerMaxRating;

    public decimal TicketPriceRoundingStep { get; set; } = DefaultTicketPriceRoundingStep;

    public decimal DynamicPricingMaxIncreasePercent { get; set; } =
        DefaultDynamicPricingMaxIncreasePercent;

    public decimal DynamicPricingMaxDecreasePercent { get; set; } =
        DefaultDynamicPricingMaxDecreasePercent;

    public decimal MinTicketPrice { get; set; } = DefaultMinTicketPrice;

    public int SalesPaceCustomMarkMinutes { get; set; } = DefaultSalesPaceCustomMarkMinutes;

    public int SuggestionHighOccupancyPercent { get; set; } =
        DefaultSuggestionHighOccupancyPercent;

    public int SuggestionLowOccupancyPercent { get; set; } = DefaultSuggestionLowOccupancyPercent;

    public int SuggestionMinShowtimes { get; set; } = DefaultSuggestionMinShowtimes;

    public int SuggestionLookbackDays { get; set; } = DefaultSuggestionLookbackDays;

    public bool DailyClosingAllowOverride { get; set; }

    public decimal TicketVatPercent { get; set; } = DefaultVatPercent;

    public decimal ComboVatPercent { get; set; } = DefaultVatPercent;

    public decimal GiftCardVatPercent { get; set; } = DefaultVatPercent;

    public decimal MembershipVatPercent { get; set; } = DefaultVatPercent;

    public int InvoiceRequestWindowDays { get; set; } = DefaultInvoiceRequestWindowDays;

    public decimal[] GiftCardDenominations { get; set; } = [.. DefaultGiftCardDenominations];

    public decimal GiftCardMinAmount { get; set; } = DefaultGiftCardMinAmount;

    public decimal GiftCardMaxAmount { get; set; } = DefaultGiftCardMaxAmount;

    public int GiftCardValidityMonths { get; set; } = DefaultGiftCardValidityMonths;

    public int GiftCardRefundExtensionDays { get; set; } = DefaultGiftCardRefundExtensionDays;

    public int GiftCardMaxFailedAttempts { get; set; } = DefaultGiftCardMaxFailedAttempts;

    public int GiftCardLockoutMinutes { get; set; } = DefaultGiftCardLockoutMinutes;

    public int ExchangeDeadlineHours { get; set; } = DefaultExchangeDeadlineHours;

    public int MaxExchangesPerOrder { get; set; } = DefaultMaxExchangesPerOrder;

    public int ExchangeCreditValidityDays { get; set; } = DefaultExchangeCreditValidityDays;

    public int PreorderComboDiscountPercent { get; set; } = DefaultPreorderComboDiscountPercent;

    public int ReminderHoursBefore { get; set; } = DefaultReminderHoursBefore;

    public int PointsExpiryReminderDays { get; set; } = DefaultPointsExpiryReminderDays;

    public int MembershipExpiryReminderDays { get; set; } = DefaultMembershipExpiryReminderDays;

    public int ReferralReferrerPoints { get; set; } = DefaultReferralReferrerPoints;

    public int ReferralReferredPoints { get; set; } = DefaultReferralReferredPoints;

    public int ReferralMonthlyRewardLimit { get; set; } = DefaultReferralMonthlyRewardLimit;

    public int SupportRequestDueHours { get; set; } = DefaultSupportRequestDueHours;

    public decimal ExceptionalRefundApprovalThreshold { get; set; } =
        DefaultExceptionalRefundApprovalThreshold;

    public int ReviewInviteMinHours { get; set; } = DefaultReviewInviteMinHours;

    public int ReviewInviteMaxHours { get; set; } = DefaultReviewInviteMaxHours;

    public int ReviewRewardPoints { get; set; } = DefaultReviewRewardPoints;

    public decimal RoomQualityAlertThreshold { get; set; } = DefaultRoomQualityAlertThreshold;

    public int RoomQualityAlertDays { get; set; } = DefaultRoomQualityAlertDays;

    public int WaitlistPriorityMinutes { get; set; } = DefaultWaitlistPriorityMinutes;

    public int ExpiredOrderStreakThreshold { get; set; } = DefaultExpiredOrderStreakThreshold;

    public int ExpiredOrderRestrictionMinutes { get; set; } = DefaultExpiredOrderRestrictionMinutes;

    public int RestrictedHoldMinutes { get; set; } = DefaultRestrictedHoldMinutes;

    public int RestrictedMaxPendingOrders { get; set; } = DefaultRestrictedMaxPendingOrders;

    public int MaxPendingOrdersPerUser { get; set; } = DefaultMaxPendingOrdersPerUser;

    public int MaxHeldSeatsPerUser { get; set; } = DefaultMaxHeldSeatsPerUser;

    public int PublicFormMaxRequests { get; set; } = DefaultPublicFormMaxRequests;

    public int PublicFormWindowMinutes { get; set; } = DefaultPublicFormWindowMinutes;

    public int AbuseHoldAbandonThreshold { get; set; } = DefaultAbuseHoldAbandonThreshold;

    public int AbuseHoldAbandonWindowMinutes { get; set; } = DefaultAbuseHoldAbandonWindowMinutes;

    public int AbusePaymentFailureThreshold { get; set; } = DefaultAbusePaymentFailureThreshold;

    public int AbusePaymentFailureWindowMinutes { get; set; } = DefaultAbusePaymentFailureWindowMinutes;

    public int VerificationCodeMinutes { get; set; } = DefaultVerificationCodeMinutes;

    public int VerificationMaxAttempts { get; set; } = DefaultVerificationMaxAttempts;

    public int RoomRentalTrackingTokenValidDays { get; set; } =
        DefaultRoomRentalTrackingTokenValidDays;

    public int RoomRentalTrackingMaxFailedAttempts { get; set; } =
        DefaultRoomRentalTrackingMaxFailedAttempts;

    public int RoomRentalTrackingLockoutMinutes { get; set; } =
        DefaultRoomRentalTrackingLockoutMinutes;

    public int RoomRentalDepositPercent { get; set; } = DefaultRoomRentalDepositPercent;

    public int RoomRentalQuoteValidDays { get; set; } = DefaultRoomRentalQuoteValidDays;

    public int RoomRentalBalanceDueDaysBeforeEvent { get; set; } = DefaultRoomRentalBalanceDueDaysBeforeEvent;

    public int RoomRentalFullRefundDays { get; set; } = DefaultRoomRentalFullRefundDays;

    public int RoomRentalPartialRefundDays { get; set; } = DefaultRoomRentalPartialRefundDays;

    public int RoomRentalPartialRefundPercent { get; set; } = DefaultRoomRentalPartialRefundPercent;

    public int MembershipFreeTicketsPerShowtime { get; set; } =
        DefaultMembershipFreeTicketsPerShowtime;

    public bool MembershipRefundStartedEnabled { get; set; } =
        DefaultMembershipRefundStartedEnabled;

    public int SegmentNewCustomerDays { get; set; } = DefaultSegmentNewCustomerDays;

    public int SegmentLapsedDays { get; set; } = DefaultSegmentLapsedDays;

    public int SegmentRegularMinOrders { get; set; } = DefaultSegmentRegularMinOrders;

    public int SegmentLookbackDays { get; set; } = DefaultSegmentLookbackDays;

    public int SegmentHighValueMinScore { get; set; } = DefaultSegmentHighValueMinScore;

    public int SegmentLtvProjectionMonths { get; set; } = DefaultSegmentLtvProjectionMonths;

    public int SegmentTierUpgradePercent { get; set; } = DefaultSegmentTierUpgradePercent;

    public int SegmentPreferenceMinViewings { get; set; } = DefaultSegmentPreferenceMinViewings;

    public int SegmentPreferenceMinSharePercent { get; set; } =
        DefaultSegmentPreferenceMinSharePercent;

    public int CampaignMinIntervalDays { get; set; } = DefaultCampaignMinIntervalDays;
}
