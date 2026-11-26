namespace Cinema.Application.Interfaces.Orders;

public interface ISeatHoldLimitService
{
    Task<int> EnsureWithinLimitsAsync(
        int userId,
        int seatCount,
        CancellationToken cancellationToken
    );
}
