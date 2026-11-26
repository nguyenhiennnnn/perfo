namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipFreeTicketHoldService
{
    Task<int> ReserveAsync(
        int userId,
        int orderId,
        int orderLineId,
        int showtimeId,
        decimal convertedAmount,
        CancellationToken cancellationToken
    );

    Task ReleaseLineAsync(int orderId, int orderLineId, CancellationToken cancellationToken);
}
