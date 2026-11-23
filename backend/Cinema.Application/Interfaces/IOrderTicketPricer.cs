using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public sealed record PricedTicketSeat(
    int ShowtimeSeatId,
    int? TicketTypeId,
    bool RequiresCardPresentation,
    decimal ListPrice
);

public interface IOrderTicketPricer
{
    Task<IReadOnlyDictionary<int, PricedTicketSeat>> PriceAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        IReadOnlyList<SeatTicketSelectionRequest>? selections,
        bool acceptPriceChange,
        CancellationToken cancellationToken
    );
}
