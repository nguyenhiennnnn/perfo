using Cinema.Application.DTOs.Promotions;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Promotions;

public static class PromotionBulkMapper
{
    public static UpsertPromotionRequest ToUpsert(
        BulkPersonalCodesRequest request,
        string code,
        int userId,
        DateTime? endsAt
    ) =>
        new(
            code,
            request.Name,
            request.Description,
            nameof(PromotionType.PersonalCode),
            request.DiscountType,
            request.DiscountValue,
            request.MaxDiscountAmount,
            request.Scope,
            request.MinOrderAmount,
            request.StartsAt,
            endsAt,
            request.Channel,
            request.MovieIds,
            request.ShowtimeIds,
            request.ScreenTypeIds,
            request.DaysOfWeek,
            request.MembershipTierIds,
            request.NewCustomersOnly,
            1,
            1,
            request.BudgetAmount,
            request.Priority,
            request.IsExclusive,
            userId
        );
}
