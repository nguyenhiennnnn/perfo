using Cinema.Application.DTOs.Orders;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderExchangeCompletionService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService giftCardHolds,
    IOrderResourceHoldService resourceHolds,
    IOrderExchangeSettlementService settlement,
    IOrderReleaseService releaseService,
    IBusinessEventDispatcher eventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    IAuditLogger auditLogger,
    ILogger<OrderExchangeCompletionService> logger
) : IOrderExchangeCompletionService
{
    public async Task<OrderExchangeCompletion> TryCompleteAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var originalOrderId =
            await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.OrderId == exchangeOrderId
                    && o.Type == OrderType.Exchange
                    && o.OriginalOrderId != null
                )
                .Select(o => o.OriginalOrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var status = OrderExchangeCompletionStatus.AwaitingPayment;
        OrderExchangeSettlementResult? settled = null;
        Completed? completed = null;

        try
        {
            await db.InTransactionAsync(
                async () =>
                {
                    await db.LockOrderAsync(originalOrderId, cancellationToken);
                    await db.LockOrderAsync(exchangeOrderId, cancellationToken);

                    var exchange = await db
                        .Orders.AsNoTracking()
                        .Where(o => o.OrderId == exchangeOrderId)
                        .Select(o => new
                        {
                            o.OrderCode,
                            o.Status,
                            o.Channel,
                            o.UserId,
                            o.CreatedByEmployeeId,
                        })
                        .FirstAsync(cancellationToken);

                    if (exchange.Status == OrderStatus.Confirmed)
                    {
                        status = OrderExchangeCompletionStatus.AlreadyCompleted;
                        return;
                    }

                    if (exchange.Status != OrderStatus.Pending)
                    {
                        status = OrderExchangeCompletionStatus.NotCompletable;
                        return;
                    }

                    var summary = await ledger.GetSummaryAsync(exchangeOrderId, cancellationToken);
                    await giftCardHolds.TrimAsync(
                        exchangeOrderId,
                        summary.FinalAmount - summary.NetPaidAmount,
                        cancellationToken
                    );
                    var giftCardReserved = await giftCardHolds.GetReservedAmountAsync(
                        exchangeOrderId,
                        cancellationToken
                    );
                    if (summary.NetPaidAmount + giftCardReserved < summary.FinalAmount)
                    {
                        status = OrderExchangeCompletionStatus.AwaitingPayment;
                        return;
                    }

                    var newLines = await db
                        .OrderLines.Include(l => l.ShowtimeSeat)
                        .ThenInclude(ss => ss!.Seat)
                        .Where(l =>
                            l.OrderId == exchangeOrderId
                            && l.LineType == OrderLineType.Ticket
                            && l.ExchangeFromLineId != null
                        )
                        .OrderBy(l => l.OrderLineId)
                        .ToListAsync(cancellationToken);
                    if (newLines.Count == 0)
                        throw new SeatsNoLongerHeldException(exchangeOrderId);

                    var oldLineIds = newLines.Select(l => l.ExchangeFromLineId!.Value).ToList();
                    var oldLines = await db
                        .OrderLines.Where(l =>
                            oldLineIds.Contains(l.OrderLineId)
                            && l.OrderId == originalOrderId
                            && l.LineType == OrderLineType.Ticket
                        )
                        .ToListAsync(cancellationToken);

                    var originalConfirmed = await db.Orders.AnyAsync(
                        o => o.OrderId == originalOrderId && o.Status == OrderStatus.Confirmed,
                        cancellationToken
                    );
                    var alreadyExchanged = await db.OrderExchanges.AnyAsync(
                        e => oldLineIds.Contains(e.OldOrderLineId),
                        cancellationToken
                    );
                    var oldShowtimeIds = oldLines.Select(l => l.ShowtimeId).Distinct().ToList();
                    var newShowtimeIds = newLines.Select(l => l.ShowtimeId).Distinct().ToList();
                    if (
                        !originalConfirmed
                        || alreadyExchanged
                        || oldLines.Count != oldLineIds.Count
                        || oldShowtimeIds.Count != 1
                        || newShowtimeIds.Count != 1
                        || oldLines.Any(l =>
                            l.IsCheckedIn || l.RefundedQuantity > 0 || l.RefundedAmount > 0m
                        )
                    )
                        throw new SeatsNoLongerHeldException(exchangeOrderId);

                    var oldShowtimeId = oldShowtimeIds[0]!.Value;
                    var newShowtimeId = newShowtimeIds[0]!.Value;
                    var now = DateTime.UtcNow;
                    var newShowtimeOpen = await db.Showtimes.AnyAsync(
                        s =>
                            s.ShowtimeId == newShowtimeId
                            && s.Status == ShowtimeStatus.Scheduled
                            && s.StartTime > now,
                        cancellationToken
                    );
                    if (!newShowtimeOpen)
                        throw new SeatsNoLongerHeldException(exchangeOrderId);

                    var newSeatIds = newLines.Select(l => l.ShowtimeSeatId!.Value).ToList();
                    var booked = await db
                        .ShowtimeSeats.Where(ss =>
                            newSeatIds.Contains(ss.ShowtimeSeatId)
                            && ss.ShowtimeId == newShowtimeId
                            && ss.Status == ShowtimeSeatStatus.Held
                            && ss.HeldByOrderId == exchangeOrderId
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
                    if (booked != newSeatIds.Count)
                        throw new SeatsNoLongerHeldException(exchangeOrderId);

                    var claimed = await db
                        .Orders.Where(o =>
                            o.OrderId == exchangeOrderId && o.Status == OrderStatus.Pending
                        )
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(o => o.Status, OrderStatus.Confirmed),
                            cancellationToken
                        );
                    if (claimed == 0)
                        throw new SeatsNoLongerHeldException(exchangeOrderId);

                    var oldSeatIds = oldLines.Select(l => l.ShowtimeSeatId!.Value).ToList();
                    await db
                        .ShowtimeSeats.Where(ss =>
                            oldSeatIds.Contains(ss.ShowtimeSeatId)
                            && ss.Status == ShowtimeSeatStatus.Booked
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                                    .SetProperty(x => x.HeldByUserId, (int?)null)
                                    .SetProperty(x => x.HeldByOrderId, (int?)null)
                                    .SetProperty(x => x.HeldUntil, (DateTime?)null),
                            cancellationToken
                        );

                    var performer =
                        performedByUserId
                        ?? await ResolvePerformerAsync(
                            exchange.Channel,
                            exchange.UserId,
                            exchange.CreatedByEmployeeId,
                            cancellationToken
                        );
                    var exchangeNumber =
                        await db
                            .OrderExchanges.AsNoTracking()
                            .Where(e => e.OrderId == originalOrderId)
                            .Select(e => e.ExchangeNumber)
                            .Distinct()
                            .CountAsync(cancellationToken) + 1;
                    var oldById = oldLines.ToDictionary(l => l.OrderLineId);

                    foreach (var line in newLines)
                    {
                        line.QrCode ??= CodeGenerator.GenerateTicketQrPayload(
                            exchange.OrderCode,
                            line.ShowtimeSeat!.Seat.SeatRow,
                            line.ShowtimeSeat.Seat.SeatNumber
                        );
                        var oldLine = oldById[line.ExchangeFromLineId!.Value];
                        oldLine.QrCode = null;
                        db.OrderExchanges.Add(
                            new OrderExchange
                            {
                                OrderId = originalOrderId,
                                ExchangeNumber = exchangeNumber,
                                OldOrderLineId = oldLine.OrderLineId,
                                NewOrderLineId = line.OrderLineId,
                                OldShowtimeId = oldShowtimeId,
                                NewShowtimeId = newShowtimeId,
                                PriceDifference = OrderPricing.RoundToDong(
                                    line.PaidAmount - oldLine.PaidAmount
                                ),
                                ExchangedAt = now,
                                PerformedByUserId = performer,
                            }
                        );
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    await resourceHolds.CommitAllAsync(exchangeOrderId, cancellationToken);

                    settled = await settlement.SettleAsync(
                        exchangeOrderId,
                        performedByUserId,
                        cancellationToken
                    );

                    auditLogger.Record(
                        AuditActions.CompleteOrderExchange,
                        AuditEntityTypes.Order,
                        exchangeOrderId.ToString(),
                        null,
                        AuditSnapshots.OrderExchangeCompleted(
                            originalOrderId,
                            exchangeNumber,
                            oldShowtimeId,
                            newShowtimeId,
                            oldLineIds,
                            [.. newLines.Select(l => l.OrderLineId)],
                            OrderPricing.RoundToDong(
                                newLines.Sum(l => l.PaidAmount) - oldLines.Sum(l => l.PaidAmount)
                            ),
                            performer
                        )
                    );
                    await db.SaveChangesAsync(cancellationToken);

                    await eventDispatcher.PublishAsync(
                        new OrderExchanged(
                            originalOrderId,
                            exchangeOrderId,
                            oldShowtimeId,
                            newShowtimeId,
                            now
                        ),
                        cancellationToken
                    );

                    completed = new Completed(oldShowtimeId, oldSeatIds, newShowtimeId, newSeatIds);
                    status = OrderExchangeCompletionStatus.Completed;
                },
                cancellationToken
            );
        }
        catch (SeatsNoLongerHeldException)
        {
            logger.LogWarning(
                "CompleteExchangeFailed ExchangeOrderId {ExchangeOrderId} OrderId {OrderId} — điều kiện đổi suất không còn hợp lệ.",
                exchangeOrderId,
                originalOrderId
            );
            await releaseService.ExpireAsync(exchangeOrderId, cancellationToken);
            return new OrderExchangeCompletion(OrderExchangeCompletionStatus.NotCompletable, null);
        }

        if (status != OrderExchangeCompletionStatus.Completed)
            return new OrderExchangeCompletion(status, null);

        await realtimeNotifier.NotifySeatBookedAsync(
            completed!.NewShowtimeId,
            completed.NewSeatIds,
            cancellationToken
        );
        await realtimeNotifier.NotifySeatReleasedAsync(
            completed.OldShowtimeId,
            completed.OldSeatIds,
            cancellationToken
        );
        logger.LogInformation(
            "OrderExchangeCompleted ExchangeOrderId {ExchangeOrderId} OrderId {OrderId} OldShowtimeId {OldShowtimeId} NewShowtimeId {NewShowtimeId} SeatCount {Count}",
            exchangeOrderId,
            originalOrderId,
            completed.OldShowtimeId,
            completed.NewShowtimeId,
            completed.NewSeatIds.Count
        );
        return new OrderExchangeCompletion(OrderExchangeCompletionStatus.Completed, settled);
    }

    private async Task<int?> ResolvePerformerAsync(
        SalesChannel channel,
        int? customerUserId,
        int? employeeId,
        CancellationToken cancellationToken
    )
    {
        if (channel != SalesChannel.Counter || employeeId is not { } id)
            return customerUserId;

        return await db
            .Employees.AsNoTracking()
            .Where(e => e.EmployeeId == id)
            .Select(e => (int?)e.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record Completed(
        int OldShowtimeId,
        List<int> OldSeatIds,
        int NewShowtimeId,
        List<int> NewSeatIds
    );
}
