using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Orders;

public sealed record ReleasedSeat(int ShowtimeId, int ShowtimeSeatId);

public interface IOrderCancellationFinalizer
{
    Task<IReadOnlyList<ReleasedSeat>> FinalizeAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        OrderRefundPlan refundPlan,
        CancellationToken cancellationToken
    );
}
