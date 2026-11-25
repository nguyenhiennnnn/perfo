using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Combos;

public interface ISupplementOrderService
{
    Task<CounterOrderResponse> CreateAsync(
        int employeeId,
        int employeeUserId,
        CounterSupplementOrderRequest request,
        CancellationToken cancellationToken
    );
}
