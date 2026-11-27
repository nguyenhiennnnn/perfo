using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Promotions;

public static class PromotionRejections
{
    public static PromotionCheckResult Invalid(
        PromotionRejectionReason reason,
        PromotionSnapshot? promotion = null
    ) => new(false, 0m, reason, ErrorCode(reason), Message(reason, promotion));

    public static PromotionRejectedException ToException(PromotionCheckResult result) =>
        new(
            result.Message ?? Message(PromotionRejectionReason.NotFound, null),
            result.ErrorCode ?? ErrorCode(PromotionRejectionReason.NotFound)
        );

    public static string ErrorCode(PromotionRejectionReason reason) =>
        reason switch
        {
            PromotionRejectionReason.NotFound => "PROMOTION_NOT_FOUND",
            PromotionRejectionReason.NotStarted => "PROMOTION_NOT_STARTED",
            PromotionRejectionReason.Expired => "PROMOTION_EXPIRED",
            PromotionRejectionReason.ChannelNotAllowed => "PROMOTION_CHANNEL_NOT_ALLOWED",
            PromotionRejectionReason.OrderTypeNotAllowed => "PROMOTION_ORDER_TYPE_NOT_ALLOWED",
            PromotionRejectionReason.ShowtimeNotEligible => "PROMOTION_SHOWTIME_NOT_ELIGIBLE",
            PromotionRejectionReason.DayNotEligible => "PROMOTION_DAY_NOT_ELIGIBLE",
            PromotionRejectionReason.TierNotEligible => "PROMOTION_TIER_NOT_ELIGIBLE",
            PromotionRejectionReason.NewCustomersOnly => "PROMOTION_NEW_CUSTOMERS_ONLY",
            PromotionRejectionReason.NewCustomerSharedIdentity =>
                "PROMOTION_NEW_CUSTOMER_SHARED_IDENTITY",
            PromotionRejectionReason.NotAssignedToCustomer => "PROMOTION_NOT_ASSIGNED",
            PromotionRejectionReason.MinOrderNotMet => "PROMOTION_MIN_ORDER_NOT_MET",
            PromotionRejectionReason.NothingToDiscount => "PROMOTION_NOTHING_TO_DISCOUNT",
            PromotionRejectionReason.UsageLimitReached => "PROMOTION_USAGE_LIMIT_REACHED",
            PromotionRejectionReason.CustomerLimitReached => "PROMOTION_CUSTOMER_LIMIT_REACHED",
            PromotionRejectionReason.BudgetExhausted => "PROMOTION_BUDGET_EXHAUSTED",
            PromotionRejectionReason.BundleExcluded => "PROMOTION_BUNDLE_EXCLUDED",
            PromotionRejectionReason.EventShowtimeExcluded => "PROMOTION_EVENT_SHOWTIME_EXCLUDED",
            _ => "PROMOTION_NOT_COMBINABLE",
        };

    public static string Message(PromotionRejectionReason reason, PromotionSnapshot? promotion) =>
        reason switch
        {
            PromotionRejectionReason.NotFound => Messages.Promotion_Rejected_NotFound,
            PromotionRejectionReason.NotStarted => Messages.Promotion_Rejected_NotStarted,
            PromotionRejectionReason.Expired => Messages.Promotion_Rejected_Expired,
            PromotionRejectionReason.ChannelNotAllowed => Messages.Promotion_Rejected_ChannelNotAllowed,
            PromotionRejectionReason.OrderTypeNotAllowed =>
                Messages.Promotion_Rejected_OrderTypeNotAllowed,
            PromotionRejectionReason.ShowtimeNotEligible =>
                Messages.Promotion_Rejected_ShowtimeNotEligible,
            PromotionRejectionReason.DayNotEligible => Messages.Promotion_Rejected_DayNotEligible,
            PromotionRejectionReason.TierNotEligible => Messages.Promotion_Rejected_TierNotEligible,
            PromotionRejectionReason.NewCustomersOnly => Messages.Promotion_Rejected_NewCustomersOnly,
            PromotionRejectionReason.NewCustomerSharedIdentity =>
                Messages.Promotion_Rejected_NewCustomerSharedIdentity,
            PromotionRejectionReason.NotAssignedToCustomer =>
                Messages.Promotion_Rejected_NotAssignedToCustomer,
            PromotionRejectionReason.MinOrderNotMet => string.Format(
                Messages.Promotion_Rejected_MinOrderNotMet,
                FormatMoney(promotion?.MinOrderAmount ?? 0m)
            ),
            PromotionRejectionReason.NothingToDiscount =>
                Messages.Promotion_Rejected_NothingToDiscount,
            PromotionRejectionReason.UsageLimitReached =>
                Messages.Promotion_Rejected_UsageLimitReached,
            PromotionRejectionReason.CustomerLimitReached =>
                Messages.Promotion_Rejected_CustomerLimitReached,
            PromotionRejectionReason.BudgetExhausted => Messages.Promotion_Rejected_BudgetExhausted,
            PromotionRejectionReason.BundleExcluded => Messages.Promotion_Rejected_BundleExcluded,
            PromotionRejectionReason.EventShowtimeExcluded =>
                Messages.Promotion_Rejected_EventShowtimeExcluded,
            _ => Messages.Promotion_Rejected_NotCombinable,
        };

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", CultureInfo.GetCultureInfo(RegionalDefaults.Locale))} {Messages.Common_CurrencyVnd}";
}
