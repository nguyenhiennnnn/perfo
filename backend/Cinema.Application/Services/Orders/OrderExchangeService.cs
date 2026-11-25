using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Options;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class OrderExchangeService(
    IAppDbContext db,
    IOrderExchangeEligibilityService eligibility,
    IOrderExchangeCompletionService completionService,
    IOrderReleaseService releaseService,
    IOrderTicketPricer ticketPricer,
    IEnumerable<IOrderHoldPolicy> holdPolicies,
    IRealtimeNotifier realtimeNotifier,
    IAuditLogger auditLogger,
    IAppCache cache,
    IOptions<BookingOptions> bookingOptions,
    ILogger<OrderExchangeService> logger
) : IOrderExchangeService
{
    public Task<StartOrderExchangeResponse> StartForCustomerAsync(
        int userId,
        int orderId,
        StartOrderExchangeRequest request,
        CancellationToken cancellationToken
    ) =>
        StartAsync(
            orderId,
            new StartContext(SalesChannel.Online, userId, userId, null, null),
            request,
            cancellationToken
        );

    public async Task<StartOrderExchangeResponse> StartAtCounterAsync(
        int employeeId,
        int employeeUserId,
        int orderId,
        StartOrderExchangeRequest request,
        CancellationToken cancellationToken
    )
    {
        var counterShiftId = await db.RequireOpenCounterShiftIdByEmployeeAsync(
            employeeId,
            cancellationToken
        );
        return await StartAsync(
            orderId,
            new StartContext(
                SalesChannel.Counter,
                employeeUserId,
                null,
                employeeId,
                counterShiftId
            ),
            request,
            cancellationToken
        );
    }

    private async Task<StartOrderExchangeResponse> StartAsync(
        int orderId,
        StartContext context,
        StartOrderExchangeRequest request,
        CancellationToken cancellationToken
    )
    {
        var seats = request.Seats ?? [];
        if (
            seats.Count == 0
            || seats.Select(s => s.OrderLineId).Distinct().Count() != seats.Count
            || seats.Select(s => s.NewShowtimeSeatId).Distinct().Count() != seats.Count
        )
            throw new BusinessRuleException(Messages.Rule_ExchangeSeatSelectionInvalid);

        var original =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.UserId,
                    o.CustomerName,
                    o.CustomerPhone,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (context.CustomerUserId is { } customerId && original.UserId != customerId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var newSeatIds = seats.Select(s => s.NewShowtimeSeatId).ToList();
        var gapRuleMode =
            context.Channel == SalesChannel.Counter
                ? SeatGapRuleMode.Off
                : await db.LoadSeatGapRuleModeAsync(cancellationToken);
        var slots = await db.LoadSeatSlotsAsync(request.NewShowtimeId, cancellationToken);
        if (SeatLayoutRules.ValidateSelection(slots, newSeatIds, gapRuleMode) is { } seatError)
            throw new BusinessRuleException(seatError);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var policy = holdPolicies.First(p => p.Channel == context.Channel);
        var orderCode = await OrderCodes.GenerateUniqueAsync(
            db,
            bookingOptions.Value.BookingCodeMaxAttempts,
            cancellationToken
        );

        Order? created = null;
        List<OrderExchangeTicketPreviewDto> tickets = [];
        decimal oldAmount = 0m;
        DateTime holdUntil = default;

        await db.InTransactionAsync(
            async () =>
            {
                var rootOrderId = await db.ResolveRootOrderIdAsync(orderId, cancellationToken);
                if (rootOrderId != orderId)
                    await db.LockOrderAsync(rootOrderId, cancellationToken);
                await db.LockOrderAsync(orderId, cancellationToken);

                var lineIds = seats.Select(s => s.OrderLineId).ToList();
                var check = await eligibility.EnsureEligibleAsync(
                    new OrderExchangeCheckRequest(
                        orderId,
                        request.NewShowtimeId,
                        lineIds,
                        context.Channel
                    ),
                    cancellationToken
                );

                var now = DateTime.UtcNow;
                var familyOrderIds = await db.LoadExchangeFamilyOrderIdsAsync(
                    rootOrderId,
                    cancellationToken
                );
                var pendingExists = await db.Orders.AnyAsync(
                    o =>
                        o.OriginalOrderId != null
                        && familyOrderIds.Contains(o.OriginalOrderId.Value)
                        && o.Type == OrderType.Exchange
                        && o.Status == OrderStatus.Pending
                        && o.ExpiresAt > now,
                    cancellationToken
                );
                if (pendingExists)
                    throw new ConflictAppException(Messages.Conflict_ExchangePendingExists);

                await policy.EnsureNoConflictingPendingAsync(
                    new OrderHoldPolicyContext(
                        request.NewShowtimeId,
                        context.HolderUserId,
                        context.EmployeeId
                    ),
                    cancellationToken
                );

                var showtime = await db
                    .Showtimes.AsNoTracking()
                    .FirstAsync(s => s.ShowtimeId == request.NewShowtimeId, cancellationToken);
                var oldLines = await db
                    .OrderLines.AsNoTracking()
                    .Where(l => check.OrderLineIds.Contains(l.OrderLineId))
                    .ToDictionaryAsync(l => l.OrderLineId, cancellationToken);

                var selections = seats
                    .Select(s => new SeatTicketSelectionRequest(
                        s.NewShowtimeSeatId,
                        oldLines[s.OrderLineId].TicketTypeId,
                        s.ExpectedPrice
                    ))
                    .ToList();
                var priced = await ticketPricer.PriceAsync(
                    request.NewShowtimeId,
                    newSeatIds,
                    selections,
                    request.AcceptPriceChange,
                    cancellationToken
                );

                holdUntil = now.AddMinutes(settings.HoldDurationMinutes);
                if (holdUntil > showtime.StartTime)
                    holdUntil = showtime.StartTime;

                var staleSeatIds = (
                    await db
                        .ShowtimeSeats.AsNoTracking()
                        .Where(ss =>
                            newSeatIds.Contains(ss.ShowtimeSeatId)
                            && ss.ShowtimeId == request.NewShowtimeId
                            && ss.Status == ShowtimeSeatStatus.Held
                        )
                        .Select(ss => ss.ShowtimeSeatId)
                        .ToListAsync(cancellationToken)
                ).ToHashSet();

                var affected = await db
                    .ShowtimeSeats.Where(ss =>
                        newSeatIds.Contains(ss.ShowtimeSeatId)
                        && ss.ShowtimeId == request.NewShowtimeId
                        && (
                            ss.Status == ShowtimeSeatStatus.Available
                            || (
                                ss.Status == ShowtimeSeatStatus.Held
                                && ss.HeldByUserId == context.HolderUserId
                                && ss.HeldUntil != null
                                && ss.HeldUntil < now
                            )
                        )
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(x => x.Status, ShowtimeSeatStatus.Held)
                                .SetProperty(x => x.HeldByUserId, context.HolderUserId)
                                .SetProperty(x => x.HeldByOrderId, (int?)null)
                                .SetProperty(x => x.HeldUntil, holdUntil),
                        cancellationToken
                    );

                if (affected != newSeatIds.Count)
                {
                    var heldSeatIds = await db
                        .ShowtimeSeats.Where(ss =>
                            newSeatIds.Contains(ss.ShowtimeSeatId)
                            && ss.ShowtimeId == request.NewShowtimeId
                            && ss.HeldByUserId == context.HolderUserId
                            && ss.Status == ShowtimeSeatStatus.Held
                            && ss.HeldUntil == holdUntil
                        )
                        .Select(ss => ss.ShowtimeSeatId)
                        .ToListAsync(cancellationToken);
                    var conflictSeatIds = newSeatIds.Except(heldSeatIds).ToList();
                    var hasBlockedConflict = await db.ShowtimeSeats.AnyAsync(
                        ss =>
                            conflictSeatIds.Contains(ss.ShowtimeSeatId)
                            && ss.ShowtimeId == request.NewShowtimeId
                            && ss.Status == ShowtimeSeatStatus.Blocked,
                        cancellationToken
                    );
                    throw new SeatConflictException(
                        hasBlockedConflict
                            ? Messages.Conflict_SeatsBlocked
                            : Messages.Conflict_SeatsAlreadyTaken,
                        conflictSeatIds
                    );
                }

                var heldSeats = await db
                    .ShowtimeSeats.Where(ss =>
                        newSeatIds.Contains(ss.ShowtimeSeatId)
                        && ss.ShowtimeId == request.NewShowtimeId
                    )
                    .ToListAsync(cancellationToken);
                SeatHoldEventRecorder.Stage(
                    db,
                    heldSeats.Where(s => staleSeatIds.Contains(s.ShowtimeSeatId)),
                    showtime,
                    SeatHoldEventType.Expired,
                    now,
                    settings.OpeningTime,
                    settings.ClosingTime
                );
                SeatHoldEventRecorder.Stage(
                    db,
                    heldSeats,
                    showtime,
                    SeatHoldEventType.Held,
                    now,
                    settings.OpeningTime,
                    settings.ClosingTime
                );

                var order = new Order
                {
                    OrderCode = orderCode,
                    Type = OrderType.Exchange,
                    Channel = context.Channel,
                    UserId = original.UserId,
                    CustomerName = original.CustomerName,
                    CustomerPhone = original.CustomerPhone,
                    Status = OrderStatus.Pending,
                    CreatedAt = now,
                    ExpiresAt = holdUntil,
                    CreatedByEmployeeId = context.EmployeeId,
                    CounterShiftId = context.CounterShiftId,
                    OriginalOrderId = orderId,
                };
                foreach (var seat in seats)
                {
                    var price = priced[seat.NewShowtimeSeatId];
                    var oldLine = oldLines[seat.OrderLineId];
                    var newPrice = OrderPricing.RoundToDong(price.ListPrice);
                    order.Lines.Add(
                        new OrderLine
                        {
                            LineType = OrderLineType.Ticket,
                            ShowtimeId = request.NewShowtimeId,
                            ShowtimeSeatId = seat.NewShowtimeSeatId,
                            TicketTypeId = price.TicketTypeId,
                            RequiresCardPresentation = price.RequiresCardPresentation,
                            CardCheckStatus = CardCheckRules.InitialStatus(
                                price.RequiresCardPresentation
                            ),
                            Quantity = 1,
                            UnitListPrice = newPrice,
                            LockedUnitPrice = newPrice,
                            PaidAmount = newPrice,
                            ExchangeFromLineId = oldLine.OrderLineId,
                        }
                    );
                    tickets.Add(
                        new OrderExchangeTicketPreviewDto(
                            oldLine.OrderLineId,
                            price.TicketTypeId,
                            oldLine.PaidAmount,
                            newPrice,
                            OrderPricing.RoundToDong(newPrice - oldLine.PaidAmount)
                        )
                    );
                }

                oldAmount = tickets.Sum(t => t.PaidAmount);
                var newAmount = tickets.Sum(t => t.NewPrice);
                var amountDue = Math.Max(OrderPricing.RoundToDong(newAmount - oldAmount), 0m);
                order.TotalAmount = newAmount;
                order.FinalAmount = amountDue;
                order.DiscountAmount = newAmount - amountDue;

                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);

                await db
                    .ShowtimeSeats.Where(ss =>
                        newSeatIds.Contains(ss.ShowtimeSeatId)
                        && ss.ShowtimeId == request.NewShowtimeId
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.HeldByOrderId, order.OrderId),
                        cancellationToken
                    );

                auditLogger.Record(
                    AuditActions.Create,
                    AuditEntityTypes.Order,
                    order.OrderId.ToString(),
                    null,
                    new
                    {
                        order.OrderCode,
                        Type = nameof(OrderType.Exchange),
                        OriginalOrderId = orderId,
                        OldShowtimeId = check.OldShowtimeId,
                        NewShowtimeId = request.NewShowtimeId,
                        OldAmount = oldAmount,
                        NewAmount = newAmount,
                        AmountDue = amountDue,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);

                created = order;
            },
            cancellationToken
        );

        await realtimeNotifier.NotifySeatHeldAsync(
            request.NewShowtimeId,
            newSeatIds,
            context.HolderUserId,
            holdUntil,
            cancellationToken
        );
        logger.LogInformation(
            "ExchangeStarted ExchangeOrderId {ExchangeOrderId} OrderId {OrderId} NewShowtimeId {ShowtimeId} SeatCount {Count}",
            created!.OrderId,
            orderId,
            request.NewShowtimeId,
            newSeatIds.Count
        );

        var status = created.Status;
        OrderExchangeSettlementResult? settlement = null;
        if (created.FinalAmount == 0m)
        {
            OrderExchangeCompletion completion;
            try
            {
                completion = await completionService.TryCompleteAsync(
                    created.OrderId,
                    context.HolderUserId,
                    cancellationToken
                );
            }
            catch (BusinessRuleException)
            {
                await releaseService.ExpireAsync(created.OrderId, cancellationToken);
                throw;
            }

            if (completion.Status != OrderExchangeCompletionStatus.Completed)
                throw new ConflictAppException(Messages.Conflict_ExchangeCompletionFailed);

            status = OrderStatus.Confirmed;
            settlement = completion.Settlement;
        }

        var newTotal = created.TotalAmount;
        var difference = OrderPricing.RoundToDong(newTotal - oldAmount);
        return new StartOrderExchangeResponse(
            created.OrderId,
            created.OrderCode,
            orderId,
            status.ToDtoStatus(),
            request.NewShowtimeId,
            oldAmount,
            newTotal,
            difference,
            created.FinalAmount,
            Math.Max(-difference, 0m),
            created.ExpiresAt!.Value,
            tickets,
            settlement
        );
    }

    private sealed record StartContext(
        SalesChannel Channel,
        int HolderUserId,
        int? CustomerUserId,
        int? EmployeeId,
        int? CounterShiftId
    );
}
