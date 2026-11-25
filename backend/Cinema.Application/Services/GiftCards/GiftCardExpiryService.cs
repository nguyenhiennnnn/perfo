using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardExpiryService(IAppDbContext db, ILogger<GiftCardExpiryService> logger)
    : IGiftCardExpiryService
{
    private const int BatchSize = 100;

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        while (true)
        {
            var now = DateTime.UtcNow;
            var due = await db
                .GiftCards.AsNoTracking()
                .Where(c =>
                    (c.Status == GiftCardStatus.Sent || c.Status == GiftCardStatus.InUse)
                    && c.ExpiresAt <= now
                    && c.ReservedAmount == 0m
                )
                .OrderBy(c => c.ExpiresAt)
                .ThenBy(c => c.GiftCardId)
                .Take(BatchSize)
                .Select(c => new { c.GiftCardId, c.Balance })
                .ToListAsync(cancellationToken);

            var expired = 0;
            foreach (var card in due)
            {
                try
                {
                    if (await ExpireAsync(card.GiftCardId, card.Balance, now, cancellationToken))
                        expired++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "GiftCardExpiryFailed GiftCardId {GiftCardId}", card.GiftCardId);
                }
            }

            total += expired;
            if (expired == 0 || due.Count < BatchSize)
                break;
        }

        if (total > 0)
            logger.LogInformation("GiftCardsExpired Count {Count}", total);
        return total;
    }

    private async Task<bool> ExpireAsync(
        int giftCardId,
        decimal balance,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var claimed = false;
        await db.InTransactionAsync(
            async () =>
            {
                var updated = await db
                    .GiftCards.Where(c =>
                        c.GiftCardId == giftCardId
                        && (c.Status == GiftCardStatus.Sent || c.Status == GiftCardStatus.InUse)
                        && c.ExpiresAt <= now
                        && c.ReservedAmount == 0m
                        && c.Balance == balance
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(c => c.Balance, 0m)
                                .SetProperty(c => c.Status, GiftCardStatus.Expired),
                        cancellationToken
                    );
                if (updated == 0)
                    return;

                if (balance > 0m)
                {
                    await GiftCardLedgerWriter.AppendAsync(
                        db,
                        giftCardId,
                        null,
                        GiftCardLedgerType.Expire,
                        -balance,
                        0m,
                        now,
                        cancellationToken
                    );
                    await db.SaveChangesAsync(cancellationToken);
                }

                claimed = true;
            },
            cancellationToken
        );
        return claimed;
    }
}
