using Cinema.Application.DTOs.Orders;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderExchangePreviewService(
    IAppDbContext db,
    IPricingService pricing,
    IOrderExchangeEligibilityService eligibility
) : IOrderExchangePreviewService
{
    private const int MaxPreviewShowtimes = 100;

    public async Task<OrderExchangePreviewResponse> PreviewAsync(
        int orderId,
        OrderExchangeCaller caller,
        OrderExchangePreviewQuery query,
        CancellationToken cancellationToken
    )
    {
        var ownerId =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new { o.UserId })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (caller.CustomerUserId is { } userId && ownerId.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var requestedIds = query.OrderLineIds?.Distinct().ToList() ?? [];
        var ticketLines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeId != null
                && (requestedIds.Count == 0 || requestedIds.Contains(l.OrderLineId))
            )
            .Select(l => new { l.ShowtimeId, l.Showtime!.MovieId })
            .ToListAsync(cancellationToken);
        if (ticketLines.Count == 0)
            throw new BusinessRuleException(
                requestedIds.Count == 0
                    ? Messages.Rule_ExchangeNoTickets
                    : Messages.Rule_ExchangeTicketNotInOrder
            );

        var movieId = ticketLines[0].MovieId;
        var currentShowtimeIds = ticketLines.Select(l => l.ShowtimeId!.Value).Distinct().ToList();
        var now = DateTime.UtcNow;

        var candidates = await db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.MovieId == movieId
                && s.Status == ShowtimeStatus.Scheduled
                && s.StartTime > now
                && (
                    query.NewShowtimeId != null
                        ? s.ShowtimeId == query.NewShowtimeId
                        : !currentShowtimeIds.Contains(s.ShowtimeId)
                )
            )
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShowtimeId)
            .Take(MaxPreviewShowtimes)
            .Select(s => new Candidate(
                s.ShowtimeId,
                s.StartTime,
                s.EndTime,
                s.RoomId,
                s.Room.RoomName
            ))
            .ToListAsync(cancellationToken);

        var results = new Dictionary<int, OrderExchangeShowtimePreviewDto>();
        var eligible = new List<Candidate>();
        IReadOnlyList<int>? selectedLineIds = null;
        foreach (var candidate in candidates)
        {
            try
            {
                var check = await eligibility.EnsureEligibleAsync(
                    new OrderExchangeCheckRequest(
                        orderId,
                        candidate.ShowtimeId,
                        requestedIds.Count == 0 ? null : requestedIds,
                        caller.Channel
                    ),
                    cancellationToken
                );
                selectedLineIds ??= check.OrderLineIds;
                eligible.Add(candidate);
            }
            catch (BusinessRuleException ex)
            {
                results[candidate.ShowtimeId] = Rejected(candidate, ex.Message);
            }
        }

        if (eligible.Count > 0 && selectedLineIds is not null)
            await PriceEligibleAsync(eligible, selectedLineIds, now, results, cancellationToken);

        return new OrderExchangePreviewResponse(
            orderId,
            movieId,
            [.. candidates.Select(c => results[c.ShowtimeId])]
        );
    }

    private async Task PriceEligibleAsync(
        List<Candidate> eligible,
        IReadOnlyList<int> selectedLineIds,
        DateTime now,
        Dictionary<int, OrderExchangeShowtimePreviewDto> results,
        CancellationToken cancellationToken
    )
    {
        var oldTickets = await db
            .OrderLines.AsNoTracking()
            .Where(l => selectedLineIds.Contains(l.OrderLineId))
            .OrderBy(l => l.OrderLineId)
            .Select(l => new OldTicket(
                l.OrderLineId,
                l.TicketTypeId,
                l.ShowtimeSeat!.Seat.SeatTypeId,
                l.Quantity,
                l.PaidAmount
            ))
            .ToListAsync(cancellationToken);

        var eligibleIds = eligible.Select(c => c.ShowtimeId).ToList();
        var seatRows = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss =>
                eligibleIds.Contains(ss.ShowtimeId)
                && ss.Status == ShowtimeSeatStatus.Available
                && ss.Seat.IsActive
                && !ss.Seat.IsAisle
            )
            .OrderBy(ss => ss.Seat.SeatRow)
            .ThenBy(ss => ss.Seat.SeatNumber)
            .Select(ss => new
            {
                ss.ShowtimeId,
                ss.SeatId,
                ss.Seat.SeatTypeId,
            })
            .ToListAsync(cancellationToken);
        var seatPools = seatRows
            .GroupBy(r => (r.ShowtimeId, r.SeatTypeId))
            .ToDictionary(g => g.Key, g => g.Select(r => r.SeatId).ToList());

        foreach (var candidate in eligible)
        {
            var seatIds = new List<int>(oldTickets.Count);
            var used = new Dictionary<int?, int>();
            var seatShortage = false;
            foreach (var ticket in oldTickets)
            {
                seatPools.TryGetValue((candidate.ShowtimeId, ticket.SeatTypeId), out var pool);
                var taken = used.GetValueOrDefault(ticket.SeatTypeId);
                if (pool is null || taken >= pool.Count)
                {
                    seatShortage = true;
                    break;
                }

                seatIds.Add(pool[taken]);
                used[ticket.SeatTypeId] = taken + 1;
            }

            if (seatShortage)
            {
                results[candidate.ShowtimeId] = Rejected(
                    candidate,
                    Messages.Rule_ExchangeInsufficientSeats
                );
                continue;
            }

            IReadOnlyList<PriceQuoteDto> quotes;
            try
            {
                quotes = await pricing.QuoteBatchAsync(
                    candidate.ShowtimeId,
                    [
                        .. oldTickets.Select(
                            (t, i) => new SeatPriceRequest(seatIds[i], t.TicketTypeId)
                        ),
                    ],
                    now,
                    cancellationToken
                );
            }
            catch (NotFoundException ex)
            {
                results[candidate.ShowtimeId] = Rejected(candidate, ex.Message);
                continue;
            }

            var ineligible = quotes.FirstOrDefault(q => !q.IsEligible);
            if (ineligible is not null)
            {
                results[candidate.ShowtimeId] = Rejected(
                    candidate,
                    string.Format(
                        Messages.Rule_ExchangeTicketTypeIneligible,
                        ineligible.IneligibleReason
                    )
                );
                continue;
            }

            var tickets = oldTickets
                .Select(
                    (t, i) =>
                    {
                        var newPrice = quotes[i].ListPrice * t.Quantity;
                        return new OrderExchangeTicketPreviewDto(
                            t.OrderLineId,
                            t.TicketTypeId,
                            t.PaidAmount,
                            newPrice,
                            OrderPricing.RoundToDong(newPrice - t.PaidAmount)
                        );
                    }
                )
                .ToList();
            var oldAmount = tickets.Sum(t => t.PaidAmount);
            var newAmount = tickets.Sum(t => t.NewPrice);
            var difference = OrderPricing.RoundToDong(newAmount - oldAmount);

            results[candidate.ShowtimeId] = new OrderExchangeShowtimePreviewDto(
                candidate.ShowtimeId,
                candidate.StartTime,
                candidate.EndTime,
                candidate.RoomId,
                candidate.RoomName,
                true,
                null,
                new OrderExchangePricingDto(
                    oldAmount,
                    newAmount,
                    difference,
                    Math.Max(difference, 0m),
                    Math.Max(-difference, 0m),
                    tickets
                )
            );
        }
    }

    private static OrderExchangeShowtimePreviewDto Rejected(Candidate candidate, string reason) =>
        new(
            candidate.ShowtimeId,
            candidate.StartTime,
            candidate.EndTime,
            candidate.RoomId,
            candidate.RoomName,
            false,
            reason,
            null
        );

    private sealed record Candidate(
        int ShowtimeId,
        DateTime StartTime,
        DateTime EndTime,
        int RoomId,
        string RoomName
    );

    private sealed record OldTicket(
        int OrderLineId,
        int? TicketTypeId,
        int? SeatTypeId,
        int Quantity,
        decimal PaidAmount
    );
}
