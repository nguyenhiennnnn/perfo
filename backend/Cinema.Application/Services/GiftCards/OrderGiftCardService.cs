using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.GiftCards;

public class OrderGiftCardService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService holds,
    IOrderConfirmationService confirmationService,
    IGiftCardAttemptGuard attemptGuard,
    ILogger<OrderGiftCardService> logger
) : IOrderGiftCardService
{
    private sealed record PendingOrder(int OrderId, OrderType Type, int? UserId);

    public async Task<OrderGiftCardResponse> ApplyAsync(
        int orderId,
        OrderPromotionCaller caller,
        string code,
        CancellationToken cancellationToken
    )
    {
        await attemptGuard.EnsureNotLockedAsync(cancellationToken);

        var normalized = GiftCardCodes.Normalize(code);
        if (
            normalized is null
            || !await db.GiftCards.AnyAsync(c => c.Code == normalized, cancellationToken)
        )
        {
            await attemptGuard.RecordFailureAsync(cancellationToken);
            throw new NotFoundException(Messages.NotFound_GiftCard);
        }

        var coversOrder = false;
        decimal appliedAmount = 0m;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);
                var order = await LoadPendingOrderAsync(orderId, caller, cancellationToken);
                if (order.Type == OrderType.GiftCard)
                    throw new BusinessRuleException(Messages.Rule_GiftCardNotForGiftCardOrder);

                var card =
                    await db
                        .GiftCards.AsNoTracking()
                        .Where(c => c.Code == normalized)
                        .Select(c => new
                        {
                            c.GiftCardId,
                            c.Type,
                            c.Status,
                            c.PurchaserUserId,
                            c.Balance,
                            c.ReservedAmount,
                            c.ExpiresAt,
                        })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_GiftCard);

                if (card.Type == GiftCardType.ExchangeCredit && order.UserId != card.PurchaserUserId)
                    throw new BusinessRuleException(Messages.Rule_GiftCardExchangeCreditOwnerOnly);
                if (
                    card.Status is not (GiftCardStatus.Sent or GiftCardStatus.InUse)
                    || card.ExpiresAt <= DateTime.UtcNow
                )
                    throw new BusinessRuleException(Messages.Rule_GiftCardNotUsable);

                var reserved = await holds.ListAsync(orderId, cancellationToken);
                if (reserved.Any(e => e.GiftCardId == card.GiftCardId))
                    throw new BusinessRuleException(Messages.Rule_GiftCardAlreadyApplied);

                var summary = await ledger.GetSummaryAsync(orderId, cancellationToken);
                var due = summary.OutstandingAmount - reserved.Sum(e => e.Amount);
                if (due <= 0m)
                    throw new BusinessRuleException(Messages.Rule_GiftCardOrderFullyCovered);

                var available = card.Balance - card.ReservedAmount;
                if (available <= 0m)
                    throw new BusinessRuleException(Messages.Rule_GiftCardBalanceInsufficient);

                appliedAmount = OrderPricing.RoundToDong(Math.Min(available, due));
                await holds.ReserveAsync(
                    orderId,
                    card.GiftCardId,
                    appliedAmount,
                    cancellationToken
                );
                await FailPendingChargesAsync(orderId, cancellationToken);
                coversOrder = appliedAmount >= due;
            },
            cancellationToken
        );

        logger.LogInformation(
            "OrderGiftCardApplied OrderId {OrderId} Amount {Amount} CoversOrder {CoversOrder}",
            orderId,
            appliedAmount,
            coversOrder
        );

        if (coversOrder)
        {
            var confirmation = await confirmationService.TryConfirmAsync(
                orderId,
                cancellationToken
            );
            if (confirmation == OrderConfirmationResult.NotPayable)
                throw new BusinessRuleException(
                    caller.CustomerUserId is null && !caller.IsRoomRental
                        ? Messages.Rule_CounterOrderNotPendingOrExpired
                        : Messages.Rule_BookingNotPendingOrExpired
                );
        }

        return await BuildResponseAsync(orderId, cancellationToken);
    }

    public async Task<OrderGiftCardResponse> RemoveAsync(
        int orderId,
        OrderPromotionCaller caller,
        int giftCardId,
        CancellationToken cancellationToken
    )
    {
        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);
                await LoadPendingOrderAsync(orderId, caller, cancellationToken);

                var reserved = await holds.ListAsync(orderId, cancellationToken);
                if (reserved.All(e => e.GiftCardId != giftCardId))
                    throw new NotFoundException(Messages.NotFound_GiftCard);

                await holds.ReleaseAsync(orderId, giftCardId, cancellationToken);
                await FailPendingChargesAsync(orderId, cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "OrderGiftCardRemoved OrderId {OrderId} GiftCardId {GiftCardId}",
            orderId,
            giftCardId
        );
        return await BuildResponseAsync(orderId, cancellationToken);
    }

    private async Task<PendingOrder> LoadPendingOrderAsync(
        int orderId,
        OrderPromotionCaller caller,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.Type,
                    o.Channel,
                    o.UserId,
                    o.Status,
                    o.ExpiresAt,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (caller.IsRoomRental)
        {
            if (order.Type != OrderType.RoomRental)
                throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        }
        else if (order.Type == OrderType.RoomRental)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        else if (caller.CustomerUserId is { } customerId)
        {
            if (order.UserId != customerId)
                throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        }
        else if (order.Channel != SalesChannel.Counter)
            throw new BusinessRuleException(Messages.Rule_CounterPaymentOnlyForCounterOrder);

        if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleException(
                caller.CustomerUserId is null && !caller.IsRoomRental
                    ? Messages.Rule_CounterOrderNotPendingOrExpired
                    : Messages.Rule_BookingNotPendingOrExpired
            );

        return new PendingOrder(order.OrderId, order.Type, order.UserId);
    }

    private Task<int> FailPendingChargesAsync(int orderId, CancellationToken cancellationToken) =>
        db
            .PaymentTransactions.Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && t.Status == PaymentStatus.Pending
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                cancellationToken
            );

    private async Task<OrderGiftCardResponse> BuildResponseAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.Status,
                o.FinalAmount,
            })
            .FirstAsync(cancellationToken);

        IReadOnlyList<GiftCardHoldEntry> applied;
        decimal payable;
        if (order.Status == OrderStatus.Pending)
        {
            var summary = await ledger.GetSummaryAsync(orderId, cancellationToken);
            applied = await holds.ListAsync(orderId, cancellationToken);
            payable = Math.Max(0m, summary.OutstandingAmount - applied.Sum(e => e.Amount));
        }
        else
        {
            applied = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t =>
                    t.OrderId == orderId
                    && t.Method == PaymentMethod.GiftCard
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Success
                    && t.GiftCardId != null
                )
                .GroupBy(t => t.GiftCardId!.Value)
                .Select(g => new GiftCardHoldEntry(g.Key, g.Sum(x => x.Amount)))
                .ToListAsync(cancellationToken);
            payable = 0m;
        }

        var cardIds = applied.Select(e => e.GiftCardId).ToList();
        var cards = await db
            .GiftCards.AsNoTracking()
            .Where(c => cardIds.Contains(c.GiftCardId))
            .Select(c => new
            {
                c.GiftCardId,
                c.Code,
                c.Type,
            })
            .ToDictionaryAsync(c => c.GiftCardId, cancellationToken);

        var items = applied
            .Where(e => cards.ContainsKey(e.GiftCardId))
            .Select(e =>
            {
                var card = cards[e.GiftCardId];
                return new OrderGiftCardItemDto(
                    card.GiftCardId,
                    GiftCardCodes.Mask(card.Code),
                    card.Type.ToString(),
                    e.Amount
                );
            })
            .ToList();

        return new OrderGiftCardResponse(
            order.OrderId,
            order.OrderCode,
            order.Status.ToDtoStatus(),
            order.FinalAmount,
            items.Sum(i => i.Amount),
            payable,
            payable <= 0m,
            items
        );
    }
}
