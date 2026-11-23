using Cinema.Application.Services.Promotions;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Orders;

public sealed record OrderPricingOptions(
    string? PromotionCode = null,
    bool ApplyAutoPromotions = false
)
{
    public static readonly OrderPricingOptions NoPromotions = new();
}

public sealed record OrderPriceResult(
    PromotionCheckResult? CodeResult,
    IReadOnlyList<AppliedPromotion> AppliedPromotions
)
{
    public static readonly OrderPriceResult None = new(null, []);
}

public sealed record MembershipBenefitContext(
    Order Order,
    IReadOnlyList<OrderLine> Lines,
    IReadOnlyList<decimal> RemainingValues
);

public sealed record MembershipBenefitDiscount(
    IReadOnlyList<decimal> LineShares,
    string? ReferenceType,
    int? ReferenceId
);
