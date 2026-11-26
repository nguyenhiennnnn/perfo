namespace Cinema.Application.Interfaces.Abuse;

public interface IAbuseSignalService
{
    Task RecordHoldAbandonedAsync(int userId, int orderId, CancellationToken cancellationToken);

    Task RecordPaymentFailedAsync(int userId, int orderId, CancellationToken cancellationToken);
}
