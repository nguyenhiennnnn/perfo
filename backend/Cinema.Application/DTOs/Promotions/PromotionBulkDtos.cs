namespace Cinema.Application.DTOs.Promotions;

public record BulkPersonalCodesRequest(
    string CodePrefix,
    List<int> UserIds,
    int? ValidDays,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    string Scope,
    decimal? MinOrderAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Channel,
    List<int>? MovieIds,
    List<int>? ShowtimeIds,
    List<int>? ScreenTypeIds,
    List<int>? DaysOfWeek,
    List<int>? MembershipTierIds,
    bool NewCustomersOnly,
    decimal? BudgetAmount,
    int Priority,
    bool IsExclusive,
    bool ExcludeEventShowtimes = false
);

public record BulkPersonalCodeItemDto(
    int PromotionId,
    string Code,
    int UserId,
    string UserName,
    string UserEmail,
    DateTime? EndsAt
);

public record BulkPersonalCodesResponse(int Created, List<BulkPersonalCodeItemDto> Items);
