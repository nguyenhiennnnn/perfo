namespace Cinema.Application.DTOs.Memberships;

public record AdminMembershipSubscriptionDto(
    int MembershipSubscriptionId,
    int UserId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    int MembershipPlanId,
    string PlanName,
    int OrderId,
    string OrderCode,
    decimal PlanPrice,
    DateTime ValidFrom,
    DateTime ValidTo,
    bool IsCurrent,
    string Status,
    DateTime? RefundedAt,
    string? RefundReason,
    decimal StoppedRevenueAmount,
    DateTime CreatedAt
);

public record MembershipSubscriptionListFilter(string? Search, string? Status, int? PlanId);
