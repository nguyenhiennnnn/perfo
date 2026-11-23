using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Caching;

public sealed record PromotionSnapshot(
    int PromotionId,
    string Code,
    string Name,
    string? Description,
    PromotionType Type,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    PromotionScope Scope,
    decimal? MinOrderAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    SalesChannel? Channel,
    IReadOnlyList<int> MovieIds,
    IReadOnlyList<int> ShowtimeIds,
    IReadOnlyList<int> ScreenTypeIds,
    IReadOnlyList<int> DaysOfWeek,
    IReadOnlyList<int> MembershipTierIds,
    bool NewCustomersOnly,
    int? TotalUsageLimit,
    int? PerCustomerLimit,
    decimal? BudgetAmount,
    int Priority,
    bool IsExclusive
)
{
    public static PromotionSnapshot From(Promotion p) =>
        new(
            p.PromotionId,
            p.Code,
            p.Name,
            p.Description,
            p.Type,
            p.DiscountType,
            p.DiscountValue,
            p.MaxDiscountAmount,
            p.Scope,
            p.MinOrderAmount,
            p.StartsAt,
            p.EndsAt,
            p.Channel,
            [.. p.MovieIds],
            [.. p.ShowtimeIds],
            [.. p.ScreenTypeIds],
            [.. p.DaysOfWeek],
            [.. p.MembershipTierIds],
            p.NewCustomersOnly,
            p.TotalUsageLimit,
            p.PerCustomerLimit,
            p.BudgetAmount,
            p.Priority,
            p.IsExclusive
        );
}
