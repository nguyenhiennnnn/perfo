using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class OrderHoldService(
    IAppDbContext db,
    IRealtimeNotifier realtimeNotifier,
    IEnumerable<IOrderHoldPolicy> holdPolicies,
    IOrderResourceHoldService resourceHolds,
    IOrderTicketPricer ticketPricer,
    IOptions<BookingOptions> bookingOptions,
    ILogger<OrderHoldService> logger,
    IAppCache cache
) : IOrderHoldService
{
    public async Task<HoldOrderResponse> HoldSeatsAsync(
        int userId,
        HoldSeatsRequest request,
        CancellationToken cancellationToken
    )
    {
        await EnsureWithinBookingLimitsAsync(
            request.SeatIds.Count,
            request.Combos,
            cancellationToken
        );
        var slots = await db.LoadSeatSlotsAsync(request.ShowtimeId, cancellationToken);
        var gapRuleMode = await db.LoadSeatGapRuleModeAsync(cancellationToken);
        if (SeatLayoutRules.ValidateSelection(slots, request.SeatIds, gapRuleMode) is { } seatError)
            throw new BusinessRuleException(seatError);

        var order = await CreateHoldOrderAsync(
            request.ShowtimeId,
            request.SeatIds,
            request.Combos,
            holderUserId: userId,
            channel: SalesChannel.Online,
            orderUserId: userId,
            createdByEmployeeId: null,
            customerName: request.CustomerName,
            customerPhone: request.CustomerPhone,
            cancellationToken,
            ticketSelections: request.Tickets,
            acceptPriceChange: request.AcceptPriceChange
        );

        return new HoldOrderResponse(
            order.OrderId,
            order.OrderCode,
            order.Status.ToDtoStatus(),
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.ExpiresAt!.Value
        );
    }

    public async Task<Order> CreateHoldOrderAsync(
        int showtimeId,
        List<int> showtimeSeatIds,
        List<ComboSelectionRequest>? comboSelections,
        int holderUserId,
        SalesChannel channel,
        int? orderUserId,
        int? createdByEmployeeId,
        string customerName,
        string? customerPhone,
        CancellationToken cancellationToken,
        IReadOnlyList<SeatTicketSelectionRequest>? ticketSelections = null,
        bool acceptPriceChange = false
    )
    {
        var showtime =
            await db.Showtimes.FirstOrDefaultAsync(
                s => s.ShowtimeId == showtimeId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_Showtime);
        if (showtime.Status != ShowtimeStatus.Scheduled)
            throw new BusinessRuleException(Messages.Rule_ShowtimeUnavailableForBooking);
        if (showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ShowtimeStartedOrEnded);

        var holdSettings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        var policy = holdPolicies.First(p => p.Channel == channel);
        policy.EnsureSalesOpen(showtime, holdSettings.StopSellingMinutesBefore);

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ValidationAppException(
                Messages.Invalid_CustomerNameMissing,
                new Dictionary<string, string[]>
                {
                    ["customerName"] = [Messages.Invalid_CustomerNameRequired],
                }
            );
        }

        await policy.EnsureNoConflictingPendingAsync(
            new OrderHoldPolicyContext(showtimeId, holderUserId, createdByEmployeeId),
            cancellationToken
        );

        var comboSelectionList = await LoadComboSelectionsAsync(comboSelections, cancellationToken);
        var pricedSeats = await ticketPricer.PriceAsync(
            showtimeId,
            showtimeSeatIds,
            ticketSelections,
            acceptPriceChange,
            cancellationToken
        );
        var orderCode = await OrderCodes.GenerateUniqueAsync(
            db,
            bookingOptions.Value.BookingCodeMaxAttempts,
            cancellationToken
        );

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var holdUntil = now.AddMinutes(holdSettings.HoldDurationMinutes);
        if (holdUntil > showtime.StartTime)
            holdUntil = showtime.StartTime;
        var distinctSeatIds = showtimeSeatIds.Distinct().ToList();

        var affected = await db
            .ShowtimeSeats.Where(ss =>
                distinctSeatIds.Contains(ss.ShowtimeSeatId)
                && ss.ShowtimeId == showtimeId
                && (
                    ss.Status == ShowtimeSeatStatus.Available
                    || (
                        ss.Status == ShowtimeSeatStatus.Held
                        && ss.HeldByUserId == holderUserId
                        && ss.HeldUntil != null
                        && ss.HeldUntil < now
                    )
                )
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(x => x.Status, ShowtimeSeatStatus.Held)
                        .SetProperty(x => x.HeldByUserId, holderUserId)
                        .SetProperty(x => x.HeldByOrderId, (int?)null)
                        .SetProperty(x => x.HeldUntil, holdUntil),
                cancellationToken
            );

        if (affected != distinctSeatIds.Count)
        {
            var heldSeatIds = await db
                .ShowtimeSeats.Where(ss =>
                    distinctSeatIds.Contains(ss.ShowtimeSeatId)
                    && ss.ShowtimeId == showtimeId
                    && ss.HeldByUserId == holderUserId
                    && ss.Status == ShowtimeSeatStatus.Held
                    && ss.HeldUntil == holdUntil
                )
                .Select(ss => ss.ShowtimeSeatId)
                .ToListAsync(cancellationToken);
            var conflictSeatIds = distinctSeatIds.Except(heldSeatIds).ToList();

            await transaction.RollbackAsync(cancellationToken);
            throw new SeatConflictException(Messages.Conflict_SeatsAlreadyTaken, conflictSeatIds);
        }

        var heldSeats = await db
            .ShowtimeSeats.Where(ss =>
                distinctSeatIds.Contains(ss.ShowtimeSeatId) && ss.ShowtimeId == showtimeId
            )
            .ToListAsync(cancellationToken);

        var ticketLines = heldSeats
            .Select(seat => NewTicketLine(showtimeId, seat, pricedSeats[seat.ShowtimeSeatId]))
            .ToList();
        var comboLines = comboSelectionList.Select(x => NewComboLine(showtimeId, x)).ToList();
        var lines = ticketLines.Concat(comboLines).ToList();

        var totalAmount = OrderPricing.TotalAmount(lines);
        var tierDiscount = await ResolveTierDiscountAsync(
            orderUserId,
            ticketLines.Sum(l => l.LockedUnitPrice),
            cancellationToken
        );

        var order = new Order
        {
            OrderCode = orderCode,
            Type = OrderType.Ticket,
            Channel = channel,
            UserId = orderUserId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = holdUntil,
            CreatedByEmployeeId = createdByEmployeeId,
            TotalAmount = totalAmount,
            DiscountAmount = tierDiscount.Amount,
            FinalAmount = totalAmount - tierDiscount.Amount,
        };
        foreach (var line in lines)
            order.Lines.Add(line);

        if (tierDiscount.Amount > 0m)
            order.PriceAdjustments.Add(
                OrderPricing.BuildTierAdjustment(tierDiscount.Amount, tierDiscount.TierId, ticketLines)
            );

        OrderPricing.SettlePaidAmounts(order);

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        await resourceHolds.ReserveAllAsync(order, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await db
            .ShowtimeSeats.Where(ss =>
                distinctSeatIds.Contains(ss.ShowtimeSeatId) && ss.ShowtimeId == showtimeId
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.HeldByOrderId, order.OrderId),
                cancellationToken
            );
        await transaction.CommitAsync(cancellationToken);

        await realtimeNotifier.NotifySeatHeldAsync(
            showtimeId,
            distinctSeatIds,
            holderUserId,
            holdUntil,
            cancellationToken
        );
        logger.LogInformation(
            "SeatsHeld OrderId {OrderId} ShowtimeId {ShowtimeId} SeatCount {Count}",
            order.OrderId,
            showtimeId,
            distinctSeatIds.Count
        );

        return order;
    }

    public async Task<Order> CreateComboHoldOrderAsync(
        List<ComboSelectionRequest> comboSelections,
        int holderUserId,
        SalesChannel channel,
        int? orderUserId,
        int? createdByEmployeeId,
        string customerName,
        string? customerPhone,
        CancellationToken cancellationToken
    )
    {
        if (comboSelections.Count == 0)
            throw new ValidationAppException(
                Messages.Validation_Combos_AtLeastOne,
                new Dictionary<string, string[]>
                {
                    ["combos"] = [Messages.Validation_Combos_AtLeastOne],
                }
            );

        await EnsureWithinBookingLimitsAsync(0, comboSelections, cancellationToken);

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ValidationAppException(
                Messages.Invalid_CustomerNameMissing,
                new Dictionary<string, string[]>
                {
                    ["customerName"] = [Messages.Invalid_CustomerNameRequired],
                }
            );
        }

        var holdSettings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var policy = holdPolicies.First(p => p.Channel == channel);
        await policy.EnsureNoConflictingPendingAsync(
            new OrderHoldPolicyContext(null, holderUserId, createdByEmployeeId),
            cancellationToken
        );

        var comboSelectionList = await LoadComboSelectionsAsync(comboSelections, cancellationToken);
        var orderCode = await OrderCodes.GenerateUniqueAsync(
            db,
            bookingOptions.Value.BookingCodeMaxAttempts,
            cancellationToken
        );

        var now = DateTime.UtcNow;
        var holdUntil = now.AddMinutes(holdSettings.HoldDurationMinutes);
        var lines = comboSelectionList.Select(x => NewComboLine(null, x)).ToList();
        var totalAmount = OrderPricing.TotalAmount(lines);
        foreach (var line in lines)
            line.PaidAmount = line.LockedUnitPrice * line.Quantity;

        var order = new Order
        {
            OrderCode = orderCode,
            Type = OrderType.Combo,
            Channel = channel,
            UserId = orderUserId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = holdUntil,
            CreatedByEmployeeId = createdByEmployeeId,
            TotalAmount = totalAmount,
            DiscountAmount = 0m,
            FinalAmount = totalAmount,
        };
        foreach (var line in lines)
            order.Lines.Add(line);

        await db.InTransactionAsync(
            async () =>
            {
                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);
                await resourceHolds.ReserveAllAsync(order, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "ComboOrderCreated OrderId {OrderId} Channel {Channel} LineCount {Count}",
            order.OrderId,
            channel,
            lines.Count
        );
        return order;
    }

    public async Task EnsureWithinBookingLimitsAsync(
        int seatCount,
        List<ComboSelectionRequest>? combos,
        CancellationToken cancellationToken
    )
    {
        var limits = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var maxSeats = limits.MaxSeatsPerBooking;
        var maxCombo = limits.MaxComboQuantity;

        if (seatCount > maxSeats)
        {
            var message = string.Format(Messages.Validation_Seats_MaxPerBooking, maxSeats);
            throw new ValidationAppException(
                message,
                new Dictionary<string, string[]> { ["seatIds"] = [message] }
            );
        }

        if (combos is { Count: > 0 } && combos.Any(c => c.Quantity > maxCombo))
        {
            var message = string.Format(Messages.Validation_Quantity_Max, maxCombo);
            throw new ValidationAppException(
                message,
                new Dictionary<string, string[]> { ["combos"] = [message] }
            );
        }
    }

    private static OrderLine NewTicketLine(
        int showtimeId,
        ShowtimeSeat seat,
        PricedTicketSeat priced
    ) =>
        new()
        {
            LineType = OrderLineType.Ticket,
            ShowtimeId = showtimeId,
            ShowtimeSeatId = seat.ShowtimeSeatId,
            TicketTypeId = priced.TicketTypeId,
            RequiresCardPresentation = priced.RequiresCardPresentation,
            Quantity = 1,
            UnitListPrice = priced.ListPrice,
            LockedUnitPrice = priced.ListPrice,
        };

    private static OrderLine NewComboLine(int? showtimeId, (Combo Combo, int Quantity) selection) =>
        new()
        {
            LineType = OrderLineType.Combo,
            ShowtimeId = showtimeId,
            ComboId = selection.Combo.ComboId,
            Quantity = selection.Quantity,
            UnitListPrice = selection.Combo.Price,
            LockedUnitPrice = selection.Combo.Price,
        };

    private async Task<TierDiscount> ResolveTierDiscountAsync(
        int? orderUserId,
        decimal ticketAmount,
        CancellationToken cancellationToken
    )
    {
        if (orderUserId is null)
            return TierDiscount.None;

        var hasProfile = await db.CustomerProfiles.AnyAsync(
            p => p.UserId == orderUserId,
            cancellationToken
        );
        if (!hasProfile)
            return TierDiscount.None;

        var points = await LoyaltyWindow.SumActiveAsync(
            db,
            orderUserId.Value,
            DateTime.UtcNow,
            cancellationToken
        );
        var tiers = await cache.GetAsync(CacheKeys.MembershipTiers, cancellationToken);
        var tier = LoyaltyWindow.ResolveTier(tiers, points);
        if (tier is null)
            return TierDiscount.None;

        var amount = OrderPricing.TierDiscountAmount(ticketAmount, tier.DiscountPercent);
        return amount > 0m ? new TierDiscount(amount, tier.TierId) : TierDiscount.None;
    }

    private async Task<List<(Combo Combo, int Quantity)>> LoadComboSelectionsAsync(
        List<ComboSelectionRequest>? comboSelections,
        CancellationToken cancellationToken
    )
    {
        var result = new List<(Combo Combo, int Quantity)>();
        if (comboSelections is null || comboSelections.Count == 0)
            return result;

        var comboIds = comboSelections.Select(c => c.ComboId).Distinct().ToList();
        var combos = await db
            .Combos.AsNoTracking()
            .Where(c => comboIds.Contains(c.ComboId) && c.IsActive)
            .ToListAsync(cancellationToken);
        if (combos.Count != comboIds.Count)
            throw new ValidationAppException(
                Messages.Invalid_ComboIdsUnavailable,
                new Dictionary<string, string[]>
                {
                    ["combos"] = [Messages.Invalid_ComboIdUnavailable],
                }
            );

        var lookup = combos.ToDictionary(c => c.ComboId);
        foreach (var selection in comboSelections)
            result.Add((lookup[selection.ComboId], selection.Quantity));
        return result;
    }

    private readonly record struct TierDiscount(decimal Amount, int? TierId)
    {
        public static readonly TierDiscount None = new(0m, null);
    }
}
