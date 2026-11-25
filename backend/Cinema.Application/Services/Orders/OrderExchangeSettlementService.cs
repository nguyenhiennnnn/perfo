using Cinema.Application.Caching;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderExchangeSettlementService(
    IAppDbContext db,
    IGiftCardService giftCards,
    IOrderRefundService orderRefunds,
    IAppCache cache,
    IAuditLogger auditLogger,
    ILogger<OrderExchangeSettlementService> logger
) : IOrderExchangeSettlementService
{
    public async Task<OrderExchangeSettlementResult> SettleAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        OrderExchangeSettlementResult? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                result = await SettleLockedAsync(
                    exchangeOrderId,
                    performedByUserId,
                    cancellationToken
                );
            },
            cancellationToken
        );
        return result!;
    }

    private async Task<OrderExchangeSettlementResult> SettleLockedAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var exchange =
            await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.OrderId == exchangeOrderId
                    && o.Type == OrderType.Exchange
                    && o.OriginalOrderId != null
                )
                .Select(o => new { o.OrderCode, OriginalOrderId = o.OriginalOrderId!.Value })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        await db.LockOrderAsync(exchange.OriginalOrderId, cancellationToken);

        var newLines = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == exchangeOrderId && l.ExchangeFromLineId != null)
            .Select(l => new { l.PaidAmount, OldLineId = l.ExchangeFromLineId!.Value })
            .ToListAsync(cancellationToken);
        var oldLineIds = newLines.Select(l => l.OldLineId).ToList();
        var oldAmount = await db
            .OrderLines.AsNoTracking()
            .Where(l => oldLineIds.Contains(l.OrderLineId))
            .SumAsync(l => l.PaidAmount, cancellationToken);
        var credit = OrderPricing.RoundToDong(oldAmount - newLines.Sum(l => l.PaidAmount));
        if (credit <= 0m)
            return new OrderExchangeSettlementResult(
                OrderExchangeSettlementKind.None,
                0m,
                null,
                null,
                null,
                []
            );

        var customerUserId = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == exchange.OriginalOrderId)
            .Select(o => o.UserId)
            .FirstAsync(cancellationToken);

        return customerUserId is { } customerId
            ? await IssueCreditAsync(
                exchangeOrderId,
                exchange.OrderCode,
                customerId,
                credit,
                cancellationToken
            )
            : await RefundCashAsync(
                exchangeOrderId,
                exchange.OrderCode,
                exchange.OriginalOrderId,
                credit,
                performedByUserId,
                cancellationToken
            );
    }

    private async Task<OrderExchangeSettlementResult> IssueCreditAsync(
        int exchangeOrderId,
        string exchangeOrderCode,
        int customerUserId,
        decimal credit,
        CancellationToken cancellationToken
    )
    {
        var existing = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l =>
                l.OrderId == exchangeOrderId
                && l.EntryType == GiftCardLedgerType.Issue
                && l.GiftCard.Type == GiftCardType.ExchangeCredit
            )
            .Select(l => new
            {
                l.GiftCardId,
                l.GiftCard.Code,
                l.GiftCard.FaceValue,
                l.GiftCard.ExpiresAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return new OrderExchangeSettlementResult(
                OrderExchangeSettlementKind.ExchangeCredit,
                existing.FaceValue,
                existing.GiftCardId,
                existing.Code,
                existing.ExpiresAt,
                []
            );

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var expiresAt = DateTime.UtcNow.AddDays(settings.ExchangeCreditValidityDays);
        var card = await giftCards.IssueAsync(
            new IssueGiftCardRequest(
                GiftCardType.ExchangeCredit,
                credit,
                customerUserId,
                OrderId: exchangeOrderId,
                ExpiresAt: expiresAt
            ),
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);

        auditLogger.Record(
            AuditActions.IssueExchangeCredit,
            AuditEntityTypes.GiftCard,
            card.GiftCardId.ToString(),
            null,
            AuditSnapshots.ExchangeCreditIssued(exchangeOrderId, card, customerUserId)
        );
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExchangeCreditIssued ExchangeOrderId {ExchangeOrderId} ExchangeOrderCode {OrderCode} GiftCardId {GiftCardId}",
            exchangeOrderId,
            exchangeOrderCode,
            card.GiftCardId
        );
        return new OrderExchangeSettlementResult(
            OrderExchangeSettlementKind.ExchangeCredit,
            card.FaceValue,
            card.GiftCardId,
            card.Code,
            card.ExpiresAt,
            []
        );
    }

    private async Task<OrderExchangeSettlementResult> RefundCashAsync(
        int exchangeOrderId,
        string exchangeOrderCode,
        int originalOrderId,
        decimal credit,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var note = string.Format(Messages.Refund_ExchangeCashNote, exchangeOrderCode);
        var existing = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == originalOrderId
                && t.Type == PaymentTransactionType.Refund
                && t.Method == PaymentMethod.Cash
                && t.Note == note
            )
            .Select(t => new { t.PaymentTransactionId, t.Amount })
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            return new OrderExchangeSettlementResult(
                OrderExchangeSettlementKind.CashRefund,
                existing.Sum(t => t.Amount),
                null,
                null,
                null,
                [.. existing.Select(t => t.PaymentTransactionId)]
            );

        if (performedByUserId is not { } performer)
            throw new BusinessRuleException(Messages.Rule_CounterShiftRequired);
        var counterShiftId = await db.RequireOpenCounterShiftIdByUserAsync(
            performer,
            cancellationToken
        );

        var plan = await orderRefunds.BuildPlanForAmountAsync(
            originalOrderId,
            credit,
            cancellationToken
        );
        if (plan.Items.Any(i => i.Method != PaymentMethod.Cash))
            throw new BusinessRuleException(Messages.Rule_ExchangeGuestRefundCashOnly);

        var reservations = await orderRefunds.ReserveAsync(
            plan,
            performer,
            note,
            cancellationToken
        );

        auditLogger.Record(
            AuditActions.ExchangeCashRefund,
            AuditEntityTypes.Order,
            exchangeOrderId.ToString(),
            null,
            AuditSnapshots.ExchangeCashRefunded(
                originalOrderId,
                credit,
                counterShiftId,
                performer,
                reservations.Select(r => r.RefundTransactionId).ToList()
            )
        );
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExchangeCashRefunded ExchangeOrderId {ExchangeOrderId} OriginalOrderId {OrderId} CounterShiftId {ShiftId} Amount {Amount}",
            exchangeOrderId,
            originalOrderId,
            counterShiftId,
            credit
        );
        return new OrderExchangeSettlementResult(
            OrderExchangeSettlementKind.CashRefund,
            credit,
            null,
            null,
            null,
            [.. reservations.Select(r => r.RefundTransactionId)]
        );
    }
}
