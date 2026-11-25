using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Orders;

public sealed record OrderCancellationContext(
    Order Order,
    CancelReason Reason,
    CancelledBy CancelledBy,
    OrderRefundPlan RefundPlan
);

public interface IOrderCancellationHook
{
    Task OnCancelledAsync(
        OrderCancellationContext context,
        CancellationToken cancellationToken
    );
}
