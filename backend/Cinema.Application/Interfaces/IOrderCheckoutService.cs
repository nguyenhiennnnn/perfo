using Cinema.Application.DTOs.Bookings;

namespace Cinema.Application.Interfaces;

public interface IOrderCheckoutService
{
    Task<CheckoutResponse> CheckoutAsync(
        int userId,
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken
    );
}
