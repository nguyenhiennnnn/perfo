namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipRevenueService
{
    Task<int> RecognizeDueAsync(CancellationToken cancellationToken);
}
