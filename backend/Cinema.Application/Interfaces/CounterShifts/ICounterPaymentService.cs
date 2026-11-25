using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.CounterShifts;

public interface ICounterPaymentService
{
    Task<CounterCheckoutResponse> PayAsync(
        int orderId,
        int performedByUserId,
        CounterCheckoutRequest request,
        CancellationToken cancellationToken
    );
}
