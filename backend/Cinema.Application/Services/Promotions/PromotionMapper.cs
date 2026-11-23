using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Services.Pricing;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Promotions;

public sealed record PromotionRow(Promotion Promotion, string? UserName, string? UserEmail);

public static class PromotionMapper
{
    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static PromotionDto ToDto(PromotionRow row)
    {
        var p = row.Promotion;
        return new PromotionDto(
            p.PromotionId,
            p.Code,
            p.Name,
            p.Description,
            p.Type.ToString(),
            p.DiscountType.ToString(),
            p.DiscountValue,
            p.MaxDiscountAmount,
            p.Scope.ToString(),
            p.MinOrderAmount,
            p.StartsAt,
            p.EndsAt,
            p.Channel?.ToString(),
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
            p.IsExclusive,
            p.AssignedUserId,
            row.UserName,
            row.UserEmail,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt
        );
    }

    public static List<int> NormalizeIds(IEnumerable<int>? ids) => PricingMapper.NormalizeIds(ids);
}
