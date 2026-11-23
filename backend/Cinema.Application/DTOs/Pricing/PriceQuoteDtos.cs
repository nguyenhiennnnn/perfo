namespace Cinema.Application.DTOs.Pricing;

public static class PriceAdjustmentKinds
{
    public const string Rule = "Rule";
    public const string DynamicCap = "DynamicCap";
    public const string DynamicFloor = "DynamicFloor";
    public const string MinPrice = "MinPrice";
}

public record SeatPriceRequest(int SeatId, int? TicketTypeId);

public record PriceAdjustmentDto(
    string Kind,
    int? PricingRuleId,
    string? RuleName,
    string? RuleType,
    string? AdjustmentMode,
    decimal? Value,
    decimal Amount,
    string Reason
);

public record PriceQuoteDto(
    int ShowtimeId,
    int? SeatId,
    int? SeatTypeId,
    int? TicketTypeId,
    decimal BasePrice,
    decimal SeatMultiplier,
    decimal TicketTypeMultiplier,
    decimal ReferencePrice,
    decimal DynamicAdjustment,
    decimal ListPrice,
    bool IsEligible,
    string? IneligibleReason,
    IReadOnlyList<PriceAdjustmentDto> Adjustments
);

public record SeatPriceChangeDto(
    int ShowtimeSeatId,
    int? TicketTypeId,
    decimal? PreviousPrice,
    decimal NewPrice
);

public record PriceChangeRequiredDto(
    IReadOnlyList<SeatPriceChangeDto> Seats,
    decimal NewTicketTotal
);

public record PricingOccupancyDto(
    int TotalSeats,
    int BookedSeats,
    int HeldSeats,
    decimal Percent,
    decimal? SalesPerHour
);

public record AppliedPricingRuleDto(
    int PricingRuleId,
    string Name,
    string RuleType,
    string AdjustmentMode,
    decimal Value,
    int Priority,
    string Reason
);

public record PricingTicketTypeDto(
    int TicketTypeId,
    string Name,
    decimal PriceMultiplier,
    bool RequiresCardVerification
);

public record SeatTypePriceRowDto(
    int? SeatTypeId,
    string Name,
    decimal PriceMultiplier,
    IReadOnlyList<PriceQuoteDto> Prices
);

public record ShowtimePriceTableDto(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    int RoomId,
    string RoomName,
    DateTime StartTime,
    decimal BasePrice,
    DateTime At,
    string PricingMode,
    PricingOccupancyDto Occupancy,
    IReadOnlyList<AppliedPricingRuleDto> AppliedRules,
    IReadOnlyList<PricingTicketTypeDto> TicketTypes,
    IReadOnlyList<SeatTypePriceRowDto> Rows
);

public record TicketTypeFromPriceDto(int TicketTypeId, string Name, decimal FromPrice);

public record ShowtimeFromPriceDto(
    int ShowtimeId,
    decimal FromPrice,
    IReadOnlyList<TicketTypeFromPriceDto> TicketTypes
);
