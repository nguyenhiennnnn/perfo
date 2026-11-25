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
    public const int DefaultMovieEndGraceDays = 1;
    public const int DefaultShowtimeLookaheadDays = 7;

    public int RoomBufferMinutes { get; set; } = DefaultRoomBufferMinutes;

    public int MaxSeatsPerBooking { get; set; } = DefaultMaxSeatsPerBooking;

    public int MaxComboQuantity { get; set; } = DefaultMaxComboQuantity;

    public int LoyaltyWindowMonths { get; set; } = DefaultLoyaltyWindowMonths;

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
}
