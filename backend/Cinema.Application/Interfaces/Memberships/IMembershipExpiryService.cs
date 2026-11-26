namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipExpiryService
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken);
}
