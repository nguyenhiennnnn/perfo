using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipSubscriptionHandler(IAppDbContext db)
    : IInTransactionEventHandler<OrderConfirmed>
{
    private const int LockClassId = 21;

    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId && o.Type == OrderType.Membership)
            .Select(o => new { o.OrderId, o.UserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (order?.UserId is not { } userId)
            return;

        var planId = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == order.OrderId
                && l.LineType == OrderLineType.Fee
                && l.MembershipPlanId != null
            )
            .Select(l => l.MembershipPlanId)
            .FirstOrDefaultAsync(cancellationToken);
        if (planId is null)
            return;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {userId})",
            cancellationToken
        );

        if (await db.MembershipSubscriptions.AnyAsync(s => s.OrderId == order.OrderId, cancellationToken))
            return;

        var period = await db
            .MembershipPlans.IgnoreQueryFilters()
            .Where(p => p.MembershipPlanId == planId)
            .Select(p => p.Period)
            .FirstAsync(cancellationToken);

        var occurredAt = businessEvent.OccurredAt;
        var latestEnd = await db
            .MembershipSubscriptions.Where(s => s.UserId == userId && s.ValidTo > occurredAt)
            .MaxAsync(s => (DateTime?)s.ValidTo, cancellationToken);
        var validFrom = latestEnd ?? occurredAt;

        db.MembershipSubscriptions.Add(
            new MembershipSubscription
            {
                UserId = userId,
                MembershipPlanId = planId.Value,
                OrderId = order.OrderId,
                ValidFrom = validFrom,
                ValidTo = MembershipPlanRules.AddPeriod(validFrom, period),
                CreatedAt = DateTime.UtcNow,
            }
        );
    }
}
