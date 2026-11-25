using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderExchangeEligibilityService(
    IAppDbContext db,
    IAppCache cache,
    IEnumerable<IOrderHoldPolicy> holdPolicies
) : IOrderExchangeEligibilityService
{
    public async Task<OrderExchangeEligibility> EnsureEligibleAsync(
        OrderExchangeCheckRequest request,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        var orderStatus =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == request.OrderId)
                .Select(o => (OrderStatus?)o.Status)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (orderStatus != OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_ExchangeOrderNotConfirmed);

        if (
            request.Channel != SalesChannel.Counter
            && await IsCashOrderAsync(request.OrderId, cancellationToken)
        )
            throw new BusinessRuleException(Messages.Rule_ExchangeCashCounterOnly);

        var tickets = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == request.OrderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeId != null
            )
            .Select(l => new TicketState(
                l.OrderLineId,
                l.ShowtimeId!.Value,
                l.IsCheckedIn,
                l.RefundedQuantity > 0 || l.RefundedAmount > 0m
            ))
            .ToListAsync(cancellationToken);
        if (tickets.Count == 0)
            throw new BusinessRuleException(Messages.Rule_ExchangeNoTickets);

        var exchanges = await db
            .OrderExchanges.AsNoTracking()
            .Where(e => e.OrderId == request.OrderId)
            .Select(e => new { e.ExchangeNumber, e.OldOrderLineId })
            .ToListAsync(cancellationToken);
        var exchangedOut = exchanges.Select(e => e.OldOrderLineId).ToHashSet();

        var selected = SelectTickets(request.OrderLineIds, tickets, exchangedOut);

        var oldShowtimeIds = selected.Select(t => t.ShowtimeId).Distinct().ToList();
        if (oldShowtimeIds.Count > 1)
            throw new BusinessRuleException(Messages.Rule_ExchangeMixedShowtimes);
        var oldShowtimeId = oldShowtimeIds[0];

        var oldShowtime = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.ShowtimeId == oldShowtimeId)
            .Select(s => new
            {
                s.MovieId,
                s.StartTime,
                s.Status,
            })
            .FirstAsync(cancellationToken);

        if (await IsPrivateAsync(oldShowtimeId, cancellationToken))
            throw new BusinessRuleException(Messages.Rule_ExchangeFromPrivateShowtime);
        if (oldShowtime.Status != ShowtimeStatus.Scheduled)
            throw new BusinessRuleException(Messages.Rule_ExchangeOldShowtimeUnavailable);
        if (now > oldShowtime.StartTime.AddHours(-settings.ExchangeDeadlineHours))
            throw new BusinessRuleException(
                string.Format(Messages.Rule_ExchangeDeadlineExceeded, settings.ExchangeDeadlineHours)
            );

        var usedExchanges = exchanges.Select(e => e.ExchangeNumber).Distinct().Count();
        if (usedExchanges >= settings.MaxExchangesPerOrder)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_ExchangeLimitReached, settings.MaxExchangesPerOrder)
            );

        var newShowtime =
            await db
                .Showtimes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == request.NewShowtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);
        if (newShowtime.ShowtimeId == oldShowtimeId)
            throw new BusinessRuleException(Messages.Rule_ExchangeSameShowtime);
        if (newShowtime.MovieId != oldShowtime.MovieId)
            throw new BusinessRuleException(Messages.Rule_ExchangeDifferentMovie);
        if (await IsPrivateAsync(newShowtime.ShowtimeId, cancellationToken))
            throw new BusinessRuleException(Messages.Rule_ExchangeToPrivateShowtime);
        if (newShowtime.Status != ShowtimeStatus.Scheduled)
            throw new BusinessRuleException(Messages.Rule_ShowtimeUnavailableForBooking);
        if (newShowtime.StartTime <= now)
            throw new BusinessRuleException(Messages.Rule_ShowtimeStartedOrEnded);

        holdPolicies
            .First(p => p.Channel == request.Channel)
            .EnsureSalesOpen(newShowtime, settings.StopSellingMinutesBefore);

        return new OrderExchangeEligibility(
            request.OrderId,
            usedExchanges + 1,
            selected.Select(t => t.OrderLineId).ToList(),
            oldShowtimeId,
            newShowtime.ShowtimeId
        );
    }

    private static List<TicketState> SelectTickets(
        IReadOnlyCollection<int>? requestedIds,
        List<TicketState> tickets,
        HashSet<int> exchangedOut
    )
    {
        var requested = requestedIds?.Distinct().ToList() ?? [];
        if (requested.Count == 0)
        {
            var active = tickets
                .Where(t => !exchangedOut.Contains(t.OrderLineId) && !t.Refunded)
                .ToList();
            if (active.Count == 0)
                throw new BusinessRuleException(Messages.Rule_ExchangeNoTickets);
            if (active.Any(t => t.CheckedIn))
                throw new BusinessRuleException(Messages.Rule_ExchangeTicketCheckedIn);
            return active;
        }

        var selected = new List<TicketState>();
        foreach (var id in requested)
        {
            var ticket =
                tickets.FirstOrDefault(t => t.OrderLineId == id)
                ?? throw new BusinessRuleException(Messages.Rule_ExchangeTicketNotInOrder);
            if (exchangedOut.Contains(id))
                throw new BusinessRuleException(Messages.Rule_ExchangeTicketAlreadyExchanged);
            if (ticket.Refunded)
                throw new BusinessRuleException(Messages.Rule_ExchangeTicketRefunded);
            if (ticket.CheckedIn)
                throw new BusinessRuleException(Messages.Rule_ExchangeTicketCheckedIn);
            selected.Add(ticket);
        }

        return selected;
    }

    private Task<bool> IsCashOrderAsync(int orderId, CancellationToken cancellationToken) =>
        db.PaymentTransactions.AsNoTracking()
            .AnyAsync(
                t =>
                    t.OrderId == orderId
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Success
                    && t.Method == PaymentMethod.Cash,
                cancellationToken
            );

    private Task<bool> IsPrivateAsync(int showtimeId, CancellationToken cancellationToken) =>
        db.ShowtimeDailyStats.AsNoTracking()
            .AnyAsync(
                d => d.ShowtimeId == showtimeId && d.Kind == ShowtimeKind.Private,
                cancellationToken
            );

    private sealed record TicketState(
        int OrderLineId,
        int ShowtimeId,
        bool CheckedIn,
        bool Refunded
    );
}
