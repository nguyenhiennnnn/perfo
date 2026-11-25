using Cinema.Application.DTOs.Pricing;

namespace Cinema.Application.Interfaces.Pricing;

public interface IPricingService
{
    Task<PriceQuoteDto> QuoteAsync(
        int showtimeId,
        int seatId,
        int? ticketTypeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<PriceQuoteDto>> QuoteBatchAsync(
        int showtimeId,
        IReadOnlyCollection<SeatPriceRequest> seats,
        DateTime atUtc,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyDictionary<int, ShowtimeFromPriceDto>> GetFromPricesAsync(
        IReadOnlyCollection<int> showtimeIds,
        DateTime atUtc,
        CancellationToken cancellationToken
    );

    Task<ShowtimePriceTableDto> GetPriceTableAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    );
}
