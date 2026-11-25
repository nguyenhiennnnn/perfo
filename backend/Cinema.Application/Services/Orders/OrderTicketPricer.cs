using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderTicketPricer(IAppDbContext db, IPricingService pricingService, IAppCache cache)
    : IOrderTicketPricer
{
    public async Task<IReadOnlyDictionary<int, PricedTicketSeat>> PriceAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        IReadOnlyList<SeatTicketSelectionRequest>? selections,
        bool acceptPriceChange,
        CancellationToken cancellationToken
    )
    {
        var ids = showtimeSeatIds.Distinct().ToList();
        var seatIdByShowtimeSeat = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => ss.ShowtimeId == showtimeId && ids.Contains(ss.ShowtimeSeatId))
            .ToDictionaryAsync(ss => ss.ShowtimeSeatId, ss => ss.SeatId, cancellationToken);
        if (seatIdByShowtimeSeat.Count != ids.Count)
            throw new NotFoundException(Messages.NotFound_SeatInShowtime);

        var selectionBySeat = (selections ?? []).ToDictionary(s => s.ShowtimeSeatId);
        var requests = ids.Select(id => new SeatPriceRequest(
                seatIdByShowtimeSeat[id],
                selectionBySeat.GetValueOrDefault(id)?.TicketTypeId
            ))
            .ToList();
        var quotes = await pricingService.QuoteBatchAsync(
            showtimeId,
            requests,
            DateTime.UtcNow,
            cancellationToken
        );

        var ticketTypes = (await cache.GetAsync(CacheKeys.TicketTypes, cancellationToken))
            .ToDictionary(t => t.TicketTypeId);

        var priced = new Dictionary<int, PricedTicketSeat>(ids.Count);
        var changes = new List<SeatPriceChangeDto>();
        for (var i = 0; i < ids.Count; i++)
        {
            var quote = quotes[i];
            var ticketType = quote.TicketTypeId is { } typeId
                ? ticketTypes.GetValueOrDefault(typeId)
                    ?? throw new NotFoundException(Messages.NotFound_TicketType)
                : null;
            if (!quote.IsEligible)
                throw new BusinessRuleException(
                    string.Format(
                        Messages.Rule_TicketTypeNotEligible,
                        ticketType?.Name,
                        quote.IneligibleReason
                    )
                );

            if (
                selectionBySeat.GetValueOrDefault(ids[i])?.ExpectedPrice is { } expected
                && expected != quote.ListPrice
            )
                changes.Add(
                    new SeatPriceChangeDto(ids[i], quote.TicketTypeId, expected, quote.ListPrice)
                );

            priced[ids[i]] = new PricedTicketSeat(
                ids[i],
                quote.TicketTypeId,
                ticketType?.RequiresCardVerification ?? false,
                quote.ListPrice
            );
        }

        if (changes.Count > 0 && !acceptPriceChange)
            throw new PriceChangedException(
                Messages.Conflict_PriceChanged,
                new PriceChangeRequiredDto(changes, quotes.Sum(q => q.ListPrice))
            );

        return priced;
    }
}
