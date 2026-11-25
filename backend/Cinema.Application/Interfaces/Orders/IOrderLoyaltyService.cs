using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderLoyaltyService
{
    Task AwardAsync(int userId, int orderId, decimal paidAmount, CancellationToken cancellationToken);

    Task ReverseAsync(
        int userId,
        int orderId,
        decimal refundedAmount,
        CancellationToken cancellationToken
    );

    Task ReverseAsync(
        int userId,
        int orderId,
        decimal refundedAmount,
        CinemaSettings settings,
        CancellationToken cancellationToken
    );
}
