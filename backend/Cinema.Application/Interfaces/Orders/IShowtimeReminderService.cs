namespace Cinema.Application.Interfaces.Orders;

public interface IShowtimeReminderService
{
    Task<int> SendDueAsync(CancellationToken cancellationToken);
}
