using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OnlineHoldPolicy(IAppDbContext db) : IOrderHoldPolicy
{
    public SalesChannel Channel => SalesChannel.Online;

    public void EnsureSalesOpen(Showtime showtime, int stopSellingMinutesBefore)
    {
        if (
            ShowtimeLifecycle.IsOnlineSalesClosed(
                DateTime.UtcNow,
                showtime.StartTime,
                stopSellingMinutesBefore
            )
        )
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OnlineSalesClosed, stopSellingMinutesBefore)
            );
    }

    public async Task EnsureNoConflictingPendingAsync(
        OrderHoldPolicyContext context,
        CancellationToken cancellationToken
    )
    {
        var hasActivePending = await db.Orders.AnyAsync(
            o =>
                o.Status == OrderStatus.Pending
                && o.ExpiresAt > DateTime.UtcNow
                && o.Channel == SalesChannel.Online
                && o.UserId == context.HolderUserId
                && o.Lines.Any(l => l.ShowtimeId == context.ShowtimeId),
            cancellationToken
        );
        if (hasActivePending)
            throw new ConflictAppException(Messages.Conflict_PendingBookingExistsForShowtime);
    }
}
