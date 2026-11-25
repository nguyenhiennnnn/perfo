namespace Cinema.Application.Interfaces.Orders;

public interface IOrderLoyaltyService
{
    Task AwardForOrderAsync(int orderId, CancellationToken cancellationToken);

    Task RevokeForOrderAsync(int orderId, CancellationToken cancellationToken);
}
