namespace Cinema.Application.Interfaces.Waitlist;

public interface IWaitlistNotificationService
{
    Task<bool> NotifyHeadAsync(int showtimeId, CancellationToken cancellationToken);
}
