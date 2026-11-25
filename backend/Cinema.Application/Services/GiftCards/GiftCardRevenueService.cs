using Cinema.Application.Caching;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Invoices;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardRevenueService(IAppDbContext db, IAppCache cache, IVatService vat)
    : IGiftCardRevenueService
{
    public async Task RecognizeAsync(
        PaymentTransaction charge,
        int giftCardId,
        decimal amount,
        DateTime occurredAt,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var split = await vat.SplitGiftCardRedemptionAsync(amount, cancellationToken);

        db.GiftCardRevenueEntries.Add(
            new GiftCardRevenueEntry
            {
                GiftCardId = giftCardId,
                OrderId = charge.OrderId,
                PaymentTransaction = charge,
                EntryType = GiftCardRevenueEntryType.Recognized,
                Amount = split.GrossAmount,
                NetAmount = split.NetAmount,
                TaxAmount = split.TaxAmount,
                TaxRatePercent = split.RatePercent,
                BusinessDate = BusinessDay.Of(
                    occurredAt,
                    settings.OpeningTime,
                    settings.ClosingTime
                ),
                OccurredAt = occurredAt,
            }
        );
    }

    public async Task ReverseAsync(
        PaymentTransaction refund,
        PaymentTransaction charge,
        decimal amount,
        DateTime occurredAt,
        CancellationToken cancellationToken
    )
    {
        var recognized = await db
            .GiftCardRevenueEntries.AsNoTracking()
            .FirstOrDefaultAsync(
                e =>
                    e.PaymentTransactionId == charge.PaymentTransactionId
                    && e.EntryType == GiftCardRevenueEntryType.Recognized,
                cancellationToken
            );
        if (recognized is null)
            return;

        var reversed = await db
            .GiftCardRevenueEntries.AsNoTracking()
            .Where(e => e.RecognizedEntryId == recognized.GiftCardRevenueEntryId)
            .GroupBy(e => 1)
            .Select(g => new { Amount = g.Sum(e => e.Amount), Tax = g.Sum(e => e.TaxAmount) })
            .FirstOrDefaultAsync(cancellationToken);

        var reversedAmount = -(reversed?.Amount ?? 0m);
        var reversedTax = -(reversed?.Tax ?? 0m);
        var cumulativeAmount = Math.Min(recognized.Amount, reversedAmount + amount);
        var cumulativeTax =
            cumulativeAmount >= recognized.Amount
                ? recognized.TaxAmount
                : OrderPricing.RoundToDong(
                    recognized.TaxAmount * cumulativeAmount / recognized.Amount
                );
        var amountNow = cumulativeAmount - reversedAmount;
        var taxNow = Math.Min(amountNow, cumulativeTax - reversedTax);
        if (amountNow <= 0m)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        db.GiftCardRevenueEntries.Add(
            new GiftCardRevenueEntry
            {
                GiftCardId = recognized.GiftCardId,
                OrderId = recognized.OrderId,
                PaymentTransaction = refund,
                RecognizedEntryId = recognized.GiftCardRevenueEntryId,
                EntryType = GiftCardRevenueEntryType.Reversed,
                Amount = -amountNow,
                NetAmount = -(amountNow - taxNow),
                TaxAmount = -taxNow,
                TaxRatePercent = recognized.TaxRatePercent,
                BusinessDate = BusinessDay.Of(
                    occurredAt,
                    settings.OpeningTime,
                    settings.ClosingTime
                ),
                OccurredAt = occurredAt,
            }
        );
    }
}
