namespace Cinema.Application.DTOs.Bundles;

public record BundleItemDto(
    int BundleItemId,
    int? TicketTypeId,
    string? TicketTypeName,
    int? ComboId,
    string? ComboName,
    int Quantity
);

public record BundleDto(
    int BundleId,
    string Name,
    string? Description,
    decimal Price,
    IReadOnlyList<BundleItemDto> Items,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<int> MovieIds,
    List<int> ShowtimeIds,
    List<int> DaysOfWeek,
    List<int> SeatTypeIds,
    string? AllowedFromTime,
    string? AllowedToTime,
    int? DailyLimit,
    bool ExcludesPromotions,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertBundleItemRequest(int? TicketTypeId, int? ComboId, int Quantity);

public record UpsertBundleRequest(
    string Name,
    string? Description,
    decimal Price,
    List<UpsertBundleItemRequest>? Items,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<int>? MovieIds,
    List<int>? ShowtimeIds,
    List<int>? DaysOfWeek,
    List<int>? SeatTypeIds,
    string? AllowedFromTime,
    string? AllowedToTime,
    int? DailyLimit,
    bool ExcludesPromotions,
    bool IsActive = true
);

public record BundleListFilter(string? Search, bool? IsActive);

public record DeleteBundleResponse(int Id, bool Deleted);

public record BundleRetailQuote(decimal RetailTotal, int? ReferenceShowtimeId);

public record ApplicableBundleItemDto(
    int? TicketTypeId,
    int? ComboId,
    string Name,
    int Quantity,
    decimal RetailUnitPrice
);

public record ApplicableBundleDto(
    int BundleId,
    string Name,
    string? Description,
    decimal Price,
    int TicketCount,
    IReadOnlyList<ApplicableBundleItemDto> Items,
    decimal RetailTotal,
    decimal SavingsAmount,
    decimal SavingsPercent,
    bool ExcludesPromotions,
    int? RemainingToday,
    IReadOnlyList<int>? AllowedSeatTypeIds = null
);
