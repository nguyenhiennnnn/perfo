using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderCheckoutService
{
    Task<CheckoutResponse> CheckoutAsync(
        int userId,
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken
    );
}
