namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipFreeTicketGrantService
{
    Task<int> GrantDueAsync(CancellationToken cancellationToken);
}
