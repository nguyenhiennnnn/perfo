using Cinema.Application.DTOs.Bookings;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public interface ICounterBookingService
{
    Task<CounterHoldResponse> CounterHoldSeatsAsync(
        int employeeId,
        int employeeUserId,
        CounterHoldRequest request,
        CancellationToken cancellationToken
    );
    Task<OrderCancellationResult> CounterCancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    );
    Task<CounterCheckoutResponse> CounterCheckoutAsync(
        int bookingId,
        CounterCheckoutRequest request,
        CancellationToken cancellationToken
    );
    Task<CheckinResponse> CheckinAsync(CheckinRequest request, CancellationToken cancellationToken);
}
