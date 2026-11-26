namespace Cinema.Application.DTOs.Memberships;

public record MembershipPlanDto(
    int MembershipPlanId,
    string Name,
    string? Description,
    string Period,
    int DurationMonths,
    decimal Price,
    decimal TicketDiscountPercent,
    int FreeTicketsPerMonth,
    decimal ComboDiscountPercent,
    bool EarlyAccess,
    bool StackableWithPromotions,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertMembershipPlanRequest(
    string Name,
    string? Description,
    string Period,
    decimal Price,
    decimal TicketDiscountPercent,
    int FreeTicketsPerMonth,
    decimal ComboDiscountPercent,
    bool EarlyAccess,
    bool StackableWithPromotions,
    bool IsActive
);

public record MembershipPlanListFilter(string? Search, bool? IsActive);

public record DeleteMembershipPlanResponse(int MembershipPlanId, bool Deleted);

public record MembershipSubscriptionDto(
    int MembershipSubscriptionId,
    int MembershipPlanId,
    string PlanName,
    int OrderId,
    DateTime ValidFrom,
    DateTime ValidTo,
    bool IsCurrent
);
