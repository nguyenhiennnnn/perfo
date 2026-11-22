using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface ICounterPaymentService
{
    Task<CounterCheckoutResponse> PayAsync(
        int orderId,
        int performedByUserId,
        CounterCheckoutRequest request,
        CancellationToken cancellationToken
    );
}
