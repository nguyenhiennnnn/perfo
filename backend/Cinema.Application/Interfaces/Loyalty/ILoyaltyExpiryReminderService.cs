namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyExpiryReminderService
{
    Task<int> SendDueAsync(CancellationToken cancellationToken);
}
