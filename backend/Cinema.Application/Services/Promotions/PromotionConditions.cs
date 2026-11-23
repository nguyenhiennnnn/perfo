using Cinema.Application.Caching;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Promotions;

public static class PromotionConditions
{
    public static PromotionRejectionReason? CheckEligibility(
        PromotionSnapshot promotion,
        PromotionContext context
    )
    {
        if (promotion.StartsAt is { } startsAt && context.NowUtc < startsAt)
            return PromotionRejectionReason.NotStarted;
        if (promotion.EndsAt is { } endsAt && context.NowUtc > endsAt)
            return PromotionRejectionReason.Expired;
        if (promotion.Channel is { } channel && channel != context.Channel)
            return PromotionRejectionReason.ChannelNotAllowed;
        if (context.OrderType is not (OrderType.Ticket or OrderType.Combo))
            return PromotionRejectionReason.OrderTypeNotAllowed;
        if (
            promotion.Type == PromotionType.PersonalCode
            && (promotion.AssignedUserId is null || promotion.AssignedUserId != context.UserId)
        )
            return PromotionRejectionReason.NotAssignedToCustomer;
        if (
            !Matches(promotion.MovieIds, context.MovieId)
            || !Matches(promotion.ShowtimeIds, context.ShowtimeId)
            || !Matches(promotion.ScreenTypeIds, context.ScreenTypeId)
        )
            return PromotionRejectionReason.ShowtimeNotEligible;
        if (
            promotion.DaysOfWeek.Count > 0
            && !promotion.DaysOfWeek.Contains((int)context.BusinessDate.DayOfWeek)
        )
            return PromotionRejectionReason.DayNotEligible;
        if (!Matches(promotion.MembershipTierIds, context.MembershipTierId))
            return PromotionRejectionReason.TierNotEligible;
        if (promotion.NewCustomersOnly && context.UserId is null)
            return PromotionRejectionReason.NewCustomersOnly;
        if (promotion.MinOrderAmount is { } minOrder && context.OrderValue < minOrder)
            return PromotionRejectionReason.MinOrderNotMet;
        return null;
    }

    public static PromotionRejectionReason? CheckUsage(
        PromotionSnapshot promotion,
        PromotionUsage usage,
        bool hasCustomer
    )
    {
        if (promotion.TotalUsageLimit is { } totalLimit && usage.TotalCount >= totalLimit)
            return PromotionRejectionReason.UsageLimitReached;
        if (
            hasCustomer
            && promotion.PerCustomerLimit is { } perCustomer
            && usage.CustomerCount >= perCustomer
        )
            return PromotionRejectionReason.CustomerLimitReached;
        if (promotion.BudgetAmount is { } budget && usage.TotalAmount >= budget)
            return PromotionRejectionReason.BudgetExhausted;
        return null;
    }

    public static bool InScope(PromotionScope scope, OrderLineType lineType) =>
        scope switch
        {
            PromotionScope.TicketsOnly => lineType == OrderLineType.Ticket,
            PromotionScope.CombosOnly => lineType == OrderLineType.Combo,
            _ => true,
        };

    public static decimal ComputeDiscount(PromotionSnapshot promotion, decimal scopeValue)
    {
        if (scopeValue <= 0m)
            return 0m;

        var amount = OrderPricing.RoundToDong(
            promotion.DiscountType == PromotionDiscountType.Percent
                ? scopeValue * promotion.DiscountValue / 100m
                : promotion.DiscountValue
        );
        if (
            promotion.DiscountType == PromotionDiscountType.Percent
            && promotion.MaxDiscountAmount is { } cap
        )
            amount = Math.Min(amount, OrderPricing.RoundToDong(cap));
        return Math.Min(amount, scopeValue);
    }

    private static bool Matches(IReadOnlyList<int> allowed, int? value) =>
        allowed.Count == 0 || (value is { } v && allowed.Contains(v));
}
