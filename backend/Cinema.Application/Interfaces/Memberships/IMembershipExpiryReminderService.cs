namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipExpiryReminderService
{
    Task<int> SendDueAsync(CancellationToken cancellationToken);
}
