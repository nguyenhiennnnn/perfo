using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderConfirmationService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IOrderLoyaltyService loyalty,
    IOrderResourceHoldService resourceHolds,
    IGiftCardHoldService giftCardHolds,
    IOrderReleaseService releaseService,
    IOrderExchangeCompletionService exchangeCompletion,
    IBusinessEventDispatcher eventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    ILogger<OrderConfirmationService> logger
) : IOrderConfirmationService
{
    public async Task<OrderConfirmationResult> TryConfirmAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var orderType =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => (OrderType?)o.Type)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (orderType == OrderType.Exchange)
            return await CompleteExchangeAsync(orderId, cancellationToken);

        var result = OrderConfirmationResult.AwaitingPayment;
        var bookedSeats = new List<(int ShowtimeId, int SeatId)>();

        try
        {
            await db.InTransactionAsync(
                async () =>
                {
                    await db.LockOrderAsync(orderId, cancellationToken);

                    var order =
                        await db
                            .Orders.AsNoTracking()
                            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
                        ?? throw new NotFoundException(Messages.NotFound_Order);

                    if (order.Status == OrderStatus.Confirmed)
                    {
                        result = OrderConfirmationResult.AlreadyConfirmed;
                        return;
                    }

                    if (order.Status != OrderStatus.Pending)
                    {
                        result = OrderConfirmationResult.NotPayable;
                        return;
                    }

                    var paymentSummary = await ledger.GetSummaryAsync(orderId, cancellationToken);
                    await giftCardHolds.TrimAsync(
                        orderId,
                        paymentSummary.FinalAmount - paymentSummary.NetPaidAmount,
                        cancellationToken
                    );
                    var giftCardReserved = await giftCardHolds.GetReservedAmountAsync(
                        orderId,
                        cancellationToken
                    );
                    if (paymentSummary.NetPaidAmount + giftCardReserved < paymentSummary.FinalAmount)
                    {
                        result = OrderConfirmationResult.AwaitingPayment;
                        return;
                    }

                    var lines = await db
                        .OrderLines.Include(l => l.ShowtimeSeat)
                        .ThenInclude(ss => ss!.Seat)
                        .Where(l => l.OrderId == orderId)
                        .ToListAsync(cancellationToken);
                    var ticketLines = lines.Where(l => l.LineType == OrderLineType.Ticket).ToList();

                    if (ticketLines.Count > 0)
                    {
                        var seatIds = ticketLines.Select(l => l.ShowtimeSeatId!.Value).ToList();
                        var booked = await db
                            .ShowtimeSeats.Where(ss =>
                                seatIds.Contains(ss.ShowtimeSeatId)
                                && ss.Status == ShowtimeSeatStatus.Held
                                && ss.HeldByOrderId == orderId
                            )
                            .ExecuteUpdateAsync(
                                setters =>
                                    setters
                                        .SetProperty(x => x.Status, ShowtimeSeatStatus.Booked)
                                        .SetProperty(x => x.HeldByUserId, (int?)null)
                                        .SetProperty(x => x.HeldByOrderId, (int?)null)
                                        .SetProperty(x => x.HeldUntil, (DateTime?)null),
                                cancellationToken
                            );
                        if (booked != seatIds.Count)
                            throw new SeatsNoLongerHeldException(orderId);
                    }

                    var claimed = await db
                        .Orders.Where(o => o.OrderId == orderId && o.Status == OrderStatus.Pending)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(o => o.Status, OrderStatus.Confirmed),
                            cancellationToken
                        );
                    if (claimed == 0)
                        throw new SeatsNoLongerHeldException(orderId);

                    foreach (var line in ticketLines.Where(l => l.QrCode is null))
                        line.QrCode = CodeGenerator.GenerateTicketQrPayload(
                            order.OrderCode,
                            line.ShowtimeSeat!.Seat.SeatRow,
                            line.ShowtimeSeat.Seat.SeatNumber
                        );

                    bookedSeats.AddRange(
                        ticketLines.Select(l => (l.ShowtimeId!.Value, l.ShowtimeSeatId!.Value))
                    );

                    if (order.UserId is not null && order.Type != OrderType.GiftCard)
                        await loyalty.AwardAsync(
                            order.UserId.Value,
                            orderId,
                            lines
                                .Where(l => l.LineType is OrderLineType.Ticket or OrderLineType.Combo)
                                .Sum(l => l.PaidAmount),
                            cancellationToken
                        );

                    await resourceHolds.CommitAllAsync(orderId, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);

                    await eventDispatcher.PublishAsync(
                        new OrderConfirmed(orderId, DateTime.UtcNow),
                        cancellationToken
                    );

                    result = OrderConfirmationResult.Confirmed;
                },
                cancellationToken
            );
        }
        catch (SeatsNoLongerHeldException)
        {
            logger.LogWarning(
                "ConfirmOrderFailed_SeatsLost OrderId {OrderId} — ghế không còn được giữ cho đơn.",
                orderId
            );
            await releaseService.ExpireAsync(orderId, cancellationToken);
            return OrderConfirmationResult.NotPayable;
        }

        if (result != OrderConfirmationResult.Confirmed)
            return result;

        foreach (var group in bookedSeats.GroupBy(s => s.ShowtimeId))
            await realtimeNotifier.NotifySeatBookedAsync(
                group.Key,
                group.Select(s => s.SeatId).ToList(),
                cancellationToken
            );

        logger.LogInformation(
            "OrderConfirmed OrderId {OrderId} SeatsBooked {Count}",
            orderId,
            bookedSeats.Count
        );
        return result;
    }

    private async Task<OrderConfirmationResult> CompleteExchangeAsync(
        int exchangeOrderId,
        CancellationToken cancellationToken
    )
    {
        var completion = await exchangeCompletion.TryCompleteAsync(
            exchangeOrderId,
            null,
            cancellationToken
        );
        return completion.Status switch
        {
            OrderExchangeCompletionStatus.Completed => OrderConfirmationResult.Confirmed,
            OrderExchangeCompletionStatus.AlreadyCompleted =>
                OrderConfirmationResult.AlreadyConfirmed,
            OrderExchangeCompletionStatus.AwaitingPayment =>
                OrderConfirmationResult.AwaitingPayment,
            _ => OrderConfirmationResult.NotPayable,
        };
    }
}
