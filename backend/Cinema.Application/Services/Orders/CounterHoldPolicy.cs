using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class CounterHoldPolicy(IAppDbContext db) : IOrderHoldPolicy
{
    private const int MaxPendingPerEmployee = 5;

    public SalesChannel Channel => SalesChannel.Counter;

    public void EnsureSalesOpen(Showtime showtime, int stopSellingMinutesBefore) { }

    public async Task EnsureNoConflictingPendingAsync(
        OrderHoldPolicyContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.CreatedByEmployeeId is null)
            return;

        var pendingCount = await db.Orders.CountAsync(
            o =>
                o.CreatedByEmployeeId == context.CreatedByEmployeeId
                && o.Status == OrderStatus.Pending
                && o.ExpiresAt > DateTime.UtcNow
                && o.Channel == SalesChannel.Counter,
            cancellationToken
        );

        if (pendingCount >= MaxPendingPerEmployee)
            throw new ConflictAppException(Messages.Rule_CounterPendingLimit);
    }
}
