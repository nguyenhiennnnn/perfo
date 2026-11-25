namespace Cinema.Application.Interfaces.Waitlist;

public interface IWaitlistClosingService
{
    Task<int> CloseEndedAsync(CancellationToken cancellationToken);
}
