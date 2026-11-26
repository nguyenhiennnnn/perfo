using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Memberships;

public static class MembershipPlanMapper
{
    public static MembershipPlanDto ToDto(MembershipPlan p) =>
        new(
            p.MembershipPlanId,
            p.Name,
            p.Description,
            p.Period.ToString(),
            MembershipPlanRules.DurationMonths(p.Period),
            p.Price,
            p.TicketDiscountPercent,
            p.FreeTicketsPerMonth,
            p.ComboDiscountPercent,
            p.EarlyAccess,
            p.StackableWithPromotions,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt
        );

    public static void Apply(MembershipPlan entity, string name, UpsertMembershipPlanRequest request)
    {
        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.Period = EnumConverters.ParseEnum<MembershipPlanPeriod>(request.Period, "period");
        entity.Price = request.Price;
        entity.TicketDiscountPercent = request.TicketDiscountPercent;
        entity.FreeTicketsPerMonth = request.FreeTicketsPerMonth;
        entity.ComboDiscountPercent = request.ComboDiscountPercent;
        entity.EarlyAccess = request.EarlyAccess;
        entity.StackableWithPromotions = request.StackableWithPromotions;
        entity.IsActive = request.IsActive;
    }
}
