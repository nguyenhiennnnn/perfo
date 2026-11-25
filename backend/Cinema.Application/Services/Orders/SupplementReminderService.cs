using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Users;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class SupplementReminderService(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService
) : ISupplementReminderService
{
    public async Task RemindAsync(
        int originalOrderId,
        SupplementReminderReason reason,
        CancellationToken cancellationToken
    )
    {
        var supplements = (
            await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.OriginalOrderId == originalOrderId
                    && o.Type == OrderType.Supplement
                    && o.Status == OrderStatus.Confirmed
                )
                .OrderBy(o => o.OrderId)
                .Select(o => new
                {
                    o.OrderCode,
                    o.FinalAmount,
                    Combos = o
                        .Lines.Where(l =>
                            l.LineType == OrderLineType.Combo
                            && l.Quantity - l.RefundedQuantity - l.QuantityFulfilled > 0
                        )
                        .OrderBy(l => l.OrderLineId)
                        .Select(l => new
                        {
                            l.Combo!.Name,
                            Remaining = l.Quantity - l.RefundedQuantity - l.QuantityFulfilled,
                        })
                        .ToList(),
                })
                .ToListAsync(cancellationToken)
        )
            .Where(s => s.Combos.Count > 0)
            .ToList();
        if (supplements.Count == 0)
            return;

        var original = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == originalOrderId && o.UserId != null)
            .Select(o => new
            {
                o.OrderCode,
                o.CustomerName,
                UserId = o.UserId!.Value,
                o.User!.Email,
                o.User.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (original is null || string.IsNullOrWhiteSpace(original.Email))
            return;

        if (!await preferences.CanSendAsync(original.UserId, EmailCategory.Service, cancellationToken))
            return;

        notificationService.SendSupplementOrderReminder(
            original.Email,
            new SupplementReminderEmailData(
                original.CustomerName ?? original.FullName,
                original.OrderCode,
                reason,
                [
                    .. supplements.Select(s => new SupplementReminderOrderLine(
                        s.OrderCode,
                        s.FinalAmount,
                        [.. s.Combos.Select(c => new SupplementReminderComboLine(c.Name, c.Remaining))]
                    )),
                ]
            )
        );
    }
}
