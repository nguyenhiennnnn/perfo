using Cinema.Application.Caching;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Promotions;

public enum PromotionRejectionReason
{
    NotFound,
    NotStarted,
    Expired,
    ChannelNotAllowed,
    OrderTypeNotAllowed,
    ShowtimeNotEligible,
    DayNotEligible,
    TierNotEligible,
    NewCustomersOnly,
    NewCustomerSharedIdentity,
    NotAssignedToCustomer,
    MinOrderNotMet,
    NothingToDiscount,
    UsageLimitReached,
    CustomerLimitReached,
    BudgetExhausted,
    NotCombinable,
    BundleExcluded,
}

public sealed record PromotionLine(OrderLineType LineType, decimal Value);

public sealed record PromotionContext(
    int? OrderId,
    SalesChannel Channel,
    OrderType OrderType,
    int? UserId,
    int? MembershipTierId,
    int? ShowtimeId,
    int? MovieId,
    int? ScreenTypeId,
    DateTime NowUtc,
    DateOnly BusinessDate,
    IReadOnlyList<PromotionLine> Lines
)
{
    public decimal OrderValue => Lines.Sum(l => Math.Max(l.Value, 0m));
}

public sealed record PromotionUsage(int TotalCount, decimal TotalAmount, int CustomerCount)
{
    public static readonly PromotionUsage None = new(0, 0m, 0);
}

public sealed record PromotionCheckResult(
    bool IsValid,
    decimal DiscountAmount,
    PromotionRejectionReason? Reason,
    string? ErrorCode,
    string? Message
)
{
    public static PromotionCheckResult Valid(decimal discountAmount) =>
        new(true, discountAmount, null, null, null);
}

public sealed record EligiblePromotion(PromotionSnapshot Promotion, PromotionUsage Usage);

public sealed record AppliedPromotion(
    PromotionSnapshot Promotion,
    decimal Amount,
    IReadOnlyList<decimal> LineShares
);

public sealed record PromotionSelection(
    IReadOnlyList<AppliedPromotion> Applied,
    IReadOnlyList<(PromotionSnapshot Promotion, PromotionRejectionReason Reason)> Rejected
);

public sealed record PromotionResolution(
    IReadOnlyList<AppliedPromotion> Applied,
    PromotionCheckResult? CodeResult
)
{
    public static readonly PromotionResolution Empty = new([], null);
}
