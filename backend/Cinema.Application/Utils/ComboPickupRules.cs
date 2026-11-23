using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class ComboPickupRules
{
    public static int Remaining(int quantity, int refundedQuantity, int quantityFulfilled) =>
        Math.Max(0, quantity - refundedQuantity - quantityFulfilled);

    public static DateTime UnclaimedCutoffUtc(DateTime? utcNow = null) =>
        BusinessTime.StartOfDayUtc(BusinessTime.Today(utcNow));

    public static bool IsUnclaimed(
        OrderType type,
        OrderStatus status,
        DateTime createdAtUtc,
        int totalRemaining,
        DateTime cutoffUtc
    ) =>
        type == OrderType.Combo
        && status == OrderStatus.Confirmed
        && totalRemaining > 0
        && createdAtUtc < cutoffUtc;
}
