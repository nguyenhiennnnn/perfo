namespace Cinema.Application.Interfaces.Waitlist;

public interface IWaitlistPriorityExpiryService
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken);
}
