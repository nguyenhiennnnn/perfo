using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardDispatchService(
    IAppDbContext db,
    IBusinessEventDispatcher eventDispatcher,
    IGiftCardEmailService emailService,
    ILogger<GiftCardDispatchService> logger
) : IGiftCardDispatchService
{
    private const int BatchSize = 100;

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueIds = await db
            .GiftCards.AsNoTracking()
            .Where(c =>
                c.Type == GiftCardType.Gift
                && c.Status == GiftCardStatus.PendingSend
                && (c.SendAt == null || c.SendAt <= now)
                && c.ExpiresAt > now
            )
            .OrderBy(c => c.SendAt)
            .ThenBy(c => c.GiftCardId)
            .Take(BatchSize)
            .Select(c => c.GiftCardId)
            .ToListAsync(cancellationToken);

        var dispatched = 0;
        foreach (var id in dueIds)
        {
            try
            {
                var claimed = false;
                await db.InTransactionAsync(
                    async () =>
                    {
                        var updated = await db
                            .GiftCards.Where(c =>
                                c.GiftCardId == id && c.Status == GiftCardStatus.PendingSend
                            )
                            .ExecuteUpdateAsync(
                                setters => setters.SetProperty(c => c.Status, GiftCardStatus.Sent),
                                cancellationToken
                            );
                        if (updated == 0)
                            return;

                        await eventDispatcher.PublishAsync(
                            new GiftCardSent(id, DateTime.UtcNow),
                            cancellationToken
                        );
                        claimed = true;
                    },
                    cancellationToken
                );
                if (claimed)
                    dispatched++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "GiftCardDispatchFailed GiftCardId {GiftCardId}", id);
            }
        }

        if (dispatched > 0)
            logger.LogInformation("GiftCardsDispatched Count {Count}", dispatched);
        return dispatched;
    }

    public async Task ResendAsync(int giftCardId, CancellationToken cancellationToken)
    {
        var status =
            await db
                .GiftCards.AsNoTracking()
                .Where(c => c.GiftCardId == giftCardId && c.Type == GiftCardType.Gift)
                .Select(c => (GiftCardStatus?)c.Status)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_GiftCard);

        if (status is not (GiftCardStatus.Sent or GiftCardStatus.InUse))
            throw new BusinessRuleException(Messages.Rule_GiftCardResendNotAllowed);

        await emailService.SendToRecipientAsync(giftCardId, cancellationToken);
    }
}
