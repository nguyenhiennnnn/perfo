using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class ChargeRevenueQuery
{
    public static IQueryable<PaymentTransaction> SuccessfulChargesCompletedBetween(
        this IQueryable<PaymentTransaction> query,
        DateTime fromInclusive,
        DateTime toExclusive
    ) =>
        query.Where(t =>
            t.Type == PaymentTransactionType.Charge
            && t.Status == PaymentStatus.Success
            && t.CompletedAt != null
            && t.CompletedAt >= fromInclusive
            && t.CompletedAt < toExclusive
        );

    public static decimal NetOf(decimal amount, decimal refunded) =>
        Math.Max(0m, amount - refunded);
}
