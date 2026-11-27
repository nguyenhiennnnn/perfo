using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeEventGiftHandoverService
{
    Task<EventGiftHandoverTicketDto> LookupAsync(string qrCode, CancellationToken cancellationToken);

    Task<EventGiftHandoverResponse> HandOverAsync(
        int staffUserId,
        EventGiftHandoverRequest request,
        CancellationToken cancellationToken
    );
}
