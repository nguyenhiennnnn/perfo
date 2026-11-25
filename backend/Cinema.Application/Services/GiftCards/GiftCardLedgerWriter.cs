using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

internal static class GiftCardLedgerWriter
{
    public static async Task AppendAsync(
        IAppDbContext db,
        int giftCardId,
        int? orderId,
        GiftCardLedgerType type,
        decimal balanceChange,
        decimal reservedChange,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var state = await db
            .GiftCards.AsNoTracking()
            .Where(c => c.GiftCardId == giftCardId)
            .Select(c => new { c.Balance, c.ReservedAmount })
            .FirstAsync(cancellationToken);

        db.GiftCardLedgers.Add(
            new GiftCardLedger
            {
                GiftCardId = giftCardId,
                EntryType = type,
                BalanceChange = balanceChange,
                ReservedChange = reservedChange,
                BalanceAfter = state.Balance,
                ReservedAfter = state.ReservedAmount,
                OrderId = orderId,
                OccurredAt = now,
            }
        );
    }
}
