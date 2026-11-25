using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardCancellationService(
    IAppDbContext db,
    IGiftCardService giftCards,
    IGiftCardHoldService holds,
    IAuditLogger auditLogger,
    ILogger<GiftCardCancellationService> logger
) : IGiftCardCancellationService
{
    public async Task<CancelGiftCardResponse> CancelAsync(
        int giftCardId,
        CancelGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var reason = request.Reason.Trim();
        CancelGiftCardResponse? response = null;

        await db.InTransactionAsync(
            async () =>
            {
                var card =
                    await db
                        .GiftCards.AsNoTracking()
                        .Where(c => c.GiftCardId == giftCardId)
                        .Select(c => new
                        {
                            c.Status,
                            c.FaceValue,
                            c.PurchaserUserId,
                        })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_GiftCard);

                if (
                    card.Status
                    is not (
                        GiftCardStatus.PendingSend
                        or GiftCardStatus.Sent
                        or GiftCardStatus.InUse
                    )
                )
                    throw new BusinessRuleException(Messages.Rule_GiftCardNotCancellable);

                await ReleaseOrderHoldsAsync(giftCardId, cancellationToken);

                var state =
                    await db
                        .GiftCards.AsNoTracking()
                        .Where(c => c.GiftCardId == giftCardId)
                        .Select(c => new
                        {
                            c.Status,
                            c.Balance,
                            c.ReservedAmount,
                        })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_GiftCard);

                var balance = state.Balance;
                var partiallyUsed = balance < card.FaceValue;
                if (partiallyUsed && request.BalanceAction is null)
                    throw new BusinessRuleException(
                        Messages.Rule_GiftCardCancelBalanceActionRequired
                    );

                var action = partiallyUsed ? request.BalanceAction : null;
                var now = DateTime.UtcNow;
                var cancelled = await db
                    .GiftCards.Where(c =>
                        c.GiftCardId == giftCardId
                        && c.Status == state.Status
                        && c.ReservedAmount == 0m
                        && c.Balance == balance
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(c => c.Balance, 0m)
                                .SetProperty(c => c.Status, GiftCardStatus.Cancelled),
                        cancellationToken
                    );
                if (cancelled == 0)
                    throw new ConflictAppException(Messages.Rule_GiftCardCancelConflict);

                await GiftCardLedgerWriter.AppendAsync(
                    db,
                    giftCardId,
                    null,
                    GiftCardLedgerType.Cancel,
                    -balance,
                    0m,
                    now,
                    cancellationToken
                );

                int? creditId = null;
                if (action == GiftCardCancelBalanceAction.ConvertToCredit && balance > 0m)
                {
                    var credit = await giftCards.IssueAsync(
                        new IssueGiftCardRequest(
                            GiftCardType.ExchangeCredit,
                            balance,
                            card.PurchaserUserId
                        ),
                        cancellationToken
                    );
                    await db.SaveChangesAsync(cancellationToken);
                    creditId = credit.GiftCardId;
                }

                auditLogger.Record(
                    AuditActions.CancelGiftCard,
                    AuditEntityTypes.GiftCard,
                    giftCardId.ToString(),
                    AuditSnapshots.GiftCardBeforeCancel(state.Status, balance),
                    AuditSnapshots.GiftCardAfterCancel(reason, balance, action, creditId)
                );
                await db.SaveChangesAsync(cancellationToken);

                response = new CancelGiftCardResponse(giftCardId, balance, action, creditId);
            },
            cancellationToken
        );

        logger.LogInformation("GiftCardCancelled GiftCardId {GiftCardId}", giftCardId);
        return response!;
    }

    private async Task ReleaseOrderHoldsAsync(int giftCardId, CancellationToken cancellationToken)
    {
        var orderIds = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l =>
                l.GiftCardId == giftCardId
                && l.OrderId != null
                && (
                    l.EntryType == GiftCardLedgerType.Reserve
                    || l.EntryType == GiftCardLedgerType.Release
                    || l.EntryType == GiftCardLedgerType.Redeem
                )
            )
            .GroupBy(l => l.OrderId!.Value)
            .Where(g => g.Sum(x => x.ReservedChange) > 0m)
            .Select(g => g.Key)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in orderIds)
        {
            await db.LockOrderAsync(orderId, cancellationToken);
            await holds.ReleaseAsync(orderId, giftCardId, cancellationToken);
            await db
                .PaymentTransactions.Where(t =>
                    t.OrderId == orderId
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Pending
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                    cancellationToken
                );
        }
    }
}
