using Cinema.Application.Caching;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardRefundHandler(
    IAppDbContext db,
    IAppCache cache,
    IPaymentLedgerService ledger,
    IGiftCardRevenueService revenue
) : IGiftCardRefundHandler
{
    public async Task<PaymentTransaction> ReserveAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        amount = OrderPricing.RoundToDong(amount);
        if (amount <= 0m)
            throw new BusinessRuleException(Messages.Refund_AmountInvalid);

        if (
            charge.Type != PaymentTransactionType.Charge
            || charge.Status != PaymentStatus.Success
            || charge.Method != PaymentMethod.GiftCard
            || charge.GiftCardId is not { } giftCardId
        )
            throw new BusinessRuleException(Messages.Refund_OriginalNotRefundable);

        var refundable = await ledger.GetRefundableAmountAsync(
            charge.PaymentTransactionId,
            cancellationToken
        );
        if (amount > refundable)
            throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var extendedUntil = now.AddDays(settings.GiftCardRefundExtensionDays);

        var credited = await db
            .GiftCards.Where(c =>
                c.GiftCardId == giftCardId && c.Balance + amount <= c.FaceValue
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(c => c.Balance, c => c.Balance + amount)
                        .SetProperty(
                            c => c.ExpiresAt,
                            c =>
                                c.ExpiresAt <= now || c.Status == GiftCardStatus.Expired
                                    ? (c.ExpiresAt > extendedUntil ? c.ExpiresAt : extendedUntil)
                                    : c.ExpiresAt
                        )
                        .SetProperty(
                            c => c.Status,
                            c =>
                                c.Status == GiftCardStatus.Expired
                                || c.Status == GiftCardStatus.Depleted
                                    ? (
                                        c.Balance + amount >= c.FaceValue
                                            ? GiftCardStatus.Sent
                                            : GiftCardStatus.InUse
                                    )
                                    : c.Status
                        ),
                cancellationToken
            );
        if (credited == 0)
            throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

        await GiftCardLedgerWriter.AppendAsync(
            db,
            giftCardId,
            charge.OrderId,
            GiftCardLedgerType.Refund,
            amount,
            0m,
            now,
            cancellationToken
        );

        var refund = new PaymentTransaction
        {
            OrderId = charge.OrderId,
            Type = PaymentTransactionType.Refund,
            Method = PaymentMethod.GiftCard,
            Amount = amount,
            TransactionCode = CodeGenerator.GenerateRefundTransactionCode(charge.TransactionCode),
            OriginalTransactionId = charge.PaymentTransactionId,
            PerformedByUserId = performedByUserId,
            Note = Truncate(note),
            Status = PaymentStatus.Success,
            CreatedAt = now,
            CompletedAt = now,
            GiftCardId = giftCardId,
        };
        db.PaymentTransactions.Add(refund);
        await revenue.ReverseAsync(refund, charge, amount, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return refund;
    }

    private static string? Truncate(string? value) =>
        value is { Length: > FieldLengths.ErrorMessage } ? value[..FieldLengths.ErrorMessage] : value;
}
