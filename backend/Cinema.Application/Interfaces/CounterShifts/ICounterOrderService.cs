using Cinema.Application.DTOs.Orders;
using Cinema.Domain.Enums;
using Cinema.Application.Interfaces.Orders;


namespace Cinema.Application.Interfaces.CounterShifts;

public interface ICounterOrderService
{
    Task<CounterOrderResponse> CreateTicketOrderAsync(
        int employeeId,
        int employeeUserId,
        CounterHoldRequest request,
        CancellationToken cancellationToken
    );

    Task<CounterOrderResponse> CreateComboOrderAsync(
        int employeeId,
        int employeeUserId,
        CounterComboOrderRequest request,
        CancellationToken cancellationToken
    );

    Task<OrderCancellationResult> CancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    );
}
