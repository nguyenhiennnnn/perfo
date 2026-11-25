using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardResourceHold(IAppDbContext db, IGiftCardRevenueService revenue)
    : IResourceHold,
        IGiftCardHoldService
{
    public string ResourceType => nameof(GiftCard);

    public Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task ReserveAsync(
        int orderId,
        int giftCardId,
        decimal amount,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var reserved = await db
            .GiftCards.Where(c =>
                c.GiftCardId == giftCardId
                && (c.Status == GiftCardStatus.Sent || c.Status == GiftCardStatus.InUse)
                && c.ExpiresAt > now
                && c.Balance - c.ReservedAmount >= amount
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(c => c.ReservedAmount, c => c.ReservedAmount + amount),
                cancellationToken
            );
        if (reserved == 0)
            throw await RejectAsync(giftCardId, now, cancellationToken);

        await GiftCardLedgerWriter.AppendAsync(
            db,
            giftCardId,
            orderId,
            GiftCardLedgerType.Reserve,
            0m,
            amount,
            now,
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(int orderId, CancellationToken cancellationToken)
    {
        var entries = await ListAsync(orderId, cancellationToken);
        if (entries.Count == 0)
            return;

        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                o.OrderCode,
                o.Channel,
                o.UserId,
                o.CounterShiftId,
            })
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            var amount = entry.Amount;
            var redeemed = await db
                .GiftCards.Where(c =>
                    c.GiftCardId == entry.GiftCardId
                    && c.ReservedAmount >= amount
                    && c.Balance >= amount
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(c => c.Balance, c => c.Balance - amount)
                            .SetProperty(c => c.ReservedAmount, c => c.ReservedAmount - amount)
                            .SetProperty(
                                c => c.Status,
                                c =>
                                    c.Status == GiftCardStatus.Sent
                                    || c.Status == GiftCardStatus.InUse
                                        ? (
                                            c.Balance - amount <= 0m
                                                ? GiftCardStatus.Depleted
                                                : GiftCardStatus.InUse
                                        )
                                        : c.Status
                            ),
                    cancellationToken
                );
            if (redeemed == 0)
                throw new BusinessRuleException(Messages.Rule_GiftCardNotUsable);

            await GiftCardLedgerWriter.AppendAsync(
                db,
                entry.GiftCardId,
                orderId,
                GiftCardLedgerType.Redeem,
                -amount,
                -amount,
                now,
                cancellationToken
            );
            var charge = new PaymentTransaction
            {
                OrderId = orderId,
                Type = PaymentTransactionType.Charge,
                Method = PaymentMethod.GiftCard,
                Amount = amount,
                TransactionCode = CodeGenerator.GenerateGiftCardTransactionCode(order.OrderCode),
                Status = PaymentStatus.Success,
                CreatedAt = now,
                CompletedAt = now,
                PerformedByUserId = order.Channel == SalesChannel.Online ? order.UserId : null,
                CounterShiftId = order.CounterShiftId,
                GiftCardId = entry.GiftCardId,
            };
            db.PaymentTransactions.Add(charge);
            await revenue.RecognizeAsync(charge, entry.GiftCardId, amount, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(int orderId, CancellationToken cancellationToken)
    {
        var entries = await ListAsync(orderId, cancellationToken);
        foreach (var entry in entries)
            await ReleaseAmountAsync(orderId, entry.GiftCardId, entry.Amount, cancellationToken);
    }

    public async Task ReleaseAsync(int orderId, int giftCardId, CancellationToken cancellationToken)
    {
        var entries = await ListAsync(orderId, cancellationToken);
        foreach (var entry in entries.Where(e => e.GiftCardId == giftCardId))
            await ReleaseAmountAsync(orderId, entry.GiftCardId, entry.Amount, cancellationToken);
    }

    public async Task TrimAsync(int orderId, decimal limit, CancellationToken cancellationToken)
    {
        var entries = await ListAsync(orderId, cancellationToken);
        var excess = entries.Sum(e => e.Amount) - Math.Max(0m, limit);
        foreach (var entry in entries.OrderByDescending(e => e.GiftCardId))
        {
            if (excess <= 0m)
                break;

            var cut = Math.Min(entry.Amount, excess);
            await ReleaseAmountAsync(orderId, entry.GiftCardId, cut, cancellationToken);
            excess -= cut;
        }
    }

    public async Task<IReadOnlyList<GiftCardHoldEntry>> ListAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l =>
                l.OrderId == orderId
                && (
                    l.EntryType == GiftCardLedgerType.Reserve
                    || l.EntryType == GiftCardLedgerType.Release
                    || l.EntryType == GiftCardLedgerType.Redeem
                )
            )
            .GroupBy(l => l.GiftCardId)
            .Select(g => new GiftCardHoldEntry(g.Key, g.Sum(x => x.ReservedChange)))
            .ToListAsync(cancellationToken);

        return rows.Where(r => r.Amount > 0m).OrderBy(r => r.GiftCardId).ToList();
    }

    public async Task<decimal> GetReservedAmountAsync(
        int orderId,
        CancellationToken cancellationToken
    ) => (await ListAsync(orderId, cancellationToken)).Sum(e => e.Amount);

    private async Task ReleaseAmountAsync(
        int orderId,
        int giftCardId,
        decimal amount,
        CancellationToken cancellationToken
    )
    {
        var released = await db
            .GiftCards.Where(c => c.GiftCardId == giftCardId && c.ReservedAmount >= amount)
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(c => c.ReservedAmount, c => c.ReservedAmount - amount),
                cancellationToken
            );
        if (released == 0)
            return;

        await GiftCardLedgerWriter.AppendAsync(
            db,
            giftCardId,
            orderId,
            GiftCardLedgerType.Release,
            0m,
            -amount,
            DateTime.UtcNow,
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<BusinessRuleException> RejectAsync(
        int giftCardId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var state = await db
            .GiftCards.AsNoTracking()
            .Where(c => c.GiftCardId == giftCardId)
            .Select(c => new { c.Status, c.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        var usable =
            state is { Status: GiftCardStatus.Sent or GiftCardStatus.InUse }
            && state.ExpiresAt > now;
        return new BusinessRuleException(
            usable ? Messages.Rule_GiftCardBalanceInsufficient : Messages.Rule_GiftCardNotUsable
        );
    }
}
