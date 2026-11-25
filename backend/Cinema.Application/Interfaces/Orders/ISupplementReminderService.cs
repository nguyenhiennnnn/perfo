using Cinema.Application.Interfaces.Notifications;

namespace Cinema.Application.Interfaces.Orders;

public interface ISupplementReminderService
{
    Task RemindAsync(
        int originalOrderId,
        SupplementReminderReason reason,
        CancellationToken cancellationToken
    );
}
