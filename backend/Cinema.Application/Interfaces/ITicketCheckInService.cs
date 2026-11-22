using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface ITicketCheckInService
{
    Task<CheckinResponse> CheckInAsync(
        CheckinRequest request,
        CancellationToken cancellationToken
    );
}
