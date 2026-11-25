using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderExchangeService
{
    Task<StartOrderExchangeResponse> StartForCustomerAsync(
        int userId,
        int orderId,
        StartOrderExchangeRequest request,
        CancellationToken cancellationToken
    );

    Task<StartOrderExchangeResponse> StartAtCounterAsync(
        int employeeId,
        int employeeUserId,
        int orderId,
        StartOrderExchangeRequest request,
        CancellationToken cancellationToken
    );
}
