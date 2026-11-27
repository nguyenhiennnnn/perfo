using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

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
            p.UpdatedAt,
            p.ExcludeEventShowtimes
        );
    }

    public static void Apply(Promotion entity, string code, UpsertPromotionRequest request)
    {
        var type = EnumConverters.ParseEnum<PromotionType>(request.Type, "type");
        var discountType = EnumConverters.ParseEnum<PromotionDiscountType>(
            request.DiscountType,
            "discountType"
        );
        var isPersonal = type == PromotionType.PersonalCode;

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.Type = type;
        entity.DiscountType = discountType;
        entity.DiscountValue = request.DiscountValue;
        entity.MaxDiscountAmount =
            discountType == PromotionDiscountType.Percent ? request.MaxDiscountAmount : null;
        entity.Scope = EnumConverters.ParseEnum<PromotionScope>(request.Scope, "scope");
        entity.MinOrderAmount = request.MinOrderAmount;
        entity.StartsAt = request.StartsAt is { } startsAt ? BusinessTime.ToUtc(startsAt) : null;
        entity.EndsAt = request.EndsAt is { } endsAt ? BusinessTime.ToUtc(endsAt) : null;
        entity.Channel = string.IsNullOrWhiteSpace(request.Channel)
            ? null
            : EnumConverters.ParseEnum<SalesChannel>(request.Channel, "channel");
        entity.MovieIds = NormalizeIds(request.MovieIds);
        entity.ShowtimeIds = NormalizeIds(request.ShowtimeIds);
        entity.ScreenTypeIds = NormalizeIds(request.ScreenTypeIds);
        entity.DaysOfWeek = NormalizeIds(request.DaysOfWeek);
        entity.MembershipTierIds = NormalizeIds(request.MembershipTierIds);
        entity.NewCustomersOnly = request.NewCustomersOnly;
        entity.TotalUsageLimit = isPersonal ? 1 : request.TotalUsageLimit;
        entity.PerCustomerLimit = isPersonal ? 1 : request.PerCustomerLimit;
        entity.BudgetAmount = request.BudgetAmount;
        entity.Priority = request.Priority;
        entity.IsExclusive = request.IsExclusive;
        entity.ExcludeEventShowtimes = request.ExcludeEventShowtimes;
        entity.AssignedUserId = isPersonal ? request.AssignedUserId : null;
        entity.IsActive = request.IsActive;
    }

    public static List<int> NormalizeIds(IEnumerable<int>? ids) => PricingMapper.NormalizeIds(ids);
}
