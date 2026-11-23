namespace Cinema.Application.DTOs.Promotions;

public record PromotionDto(
    int PromotionId,
    string Code,
    string Name,
    string? Description,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    string Scope,
    decimal? MinOrderAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Channel,
    List<int> MovieIds,
    List<int> ShowtimeIds,
    List<int> ScreenTypeIds,
    List<int> DaysOfWeek,
    List<int> MembershipTierIds,
    bool NewCustomersOnly,
    int? TotalUsageLimit,
    int? PerCustomerLimit,
    decimal? BudgetAmount,
    int Priority,
    bool IsExclusive,
    int? AssignedUserId,
    string? AssignedUserName,
    string? AssignedUserEmail,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertPromotionRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
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
    int? TotalUsageLimit,
    int? PerCustomerLimit,
    decimal? BudgetAmount,
    int Priority,
    bool IsExclusive,
    int? AssignedUserId,
    bool IsActive = true
);

public record PromotionListFilter(string? Search, string? Type, bool? IsActive);

public record DeletePromotionResponse(int Id, bool Deleted);
