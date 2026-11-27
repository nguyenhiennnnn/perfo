using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipFreeTicketResourceHold(IAppDbContext db, IAppCache cache)
    : IResourceHold,
        IMembershipFreeTicketHoldService
{
    private const int LockClassId = 23;

    public string ResourceType => nameof(MembershipFreeTicket);

    public Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task<int> ReserveAsync(
        int userId,
        int orderId,
        int orderLineId,
        int showtimeId,
        decimal convertedAmount,
        CancellationToken cancellationToken
    )
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {userId})",
            cancellationToken
        );

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var onShowtime = await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .CountAsync(t => t.UserId == userId && t.ShowtimeId == showtimeId, cancellationToken);
        if (onShowtime >= settings.MembershipFreeTicketsPerShowtime)
            throw new BusinessRuleException(Messages.Rule_FreeTicketShowtimeLimitReached);

        var now = DateTime.UtcNow;
        var ticketId = await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId
                && t.OrderId == null
                && t.PeriodStart <= now
                && t.PeriodEnd > now
                && t.MembershipSubscription.Status == MembershipSubscriptionStatus.Active
                && t.MembershipSubscription.ValidTo > now
            )
            .OrderBy(t => t.PeriodEnd)
            .ThenBy(t => t.MembershipFreeTicketId)
            .Select(t => (int?)t.MembershipFreeTicketId)
            .FirstOrDefaultAsync(cancellationToken);
        if (ticketId is null)
            throw new BusinessRuleException(Messages.Rule_FreeTicketNoneAvailable);

        var claimed = await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .Where(t => t.MembershipFreeTicketId == ticketId && t.OrderId == null)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(t => t.OrderId, (int?)orderId)
                        .SetProperty(t => t.OrderLineId, (int?)orderLineId)
                        .SetProperty(t => t.ShowtimeId, (int?)showtimeId)
                        .SetProperty(t => t.ConvertedAmount, (decimal?)convertedAmount),
                cancellationToken
            );
        if (claimed == 0)
            throw new BusinessRuleException(Messages.Rule_FreeTicketNoneAvailable);

        return ticketId.Value;
    }

    public async Task CommitAsync(int orderId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.OrderId == orderId && t.UsedAt == null && t.ShowtimeId != null)
            .Select(t => new
            {
                t.MembershipFreeTicketId,
                t.MembershipSubscriptionId,
                t.MembershipSubscription.MembershipPlanId,
                ShowtimeId = t.ShowtimeId!.Value,
                Value = t.MembershipSubscription.MembershipPlan.FreeTicketConversionValue,
            })
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
            return;

        await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .Where(t => t.OrderId == orderId && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, (DateTime?)now),
                cancellationToken
            );

        var converting = pending.Where(p => p.Value > 0m).ToList();
        if (converting.Count == 0)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var businessDate = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        foreach (var item in converting)
            db.MembershipRevenueConversions.Add(
                new MembershipRevenueConversion
                {
                    MembershipFreeTicketId = item.MembershipFreeTicketId,
                    MembershipSubscriptionId = item.MembershipSubscriptionId,
                    MembershipPlanId = item.MembershipPlanId,
                    ShowtimeId = item.ShowtimeId,
                    BusinessDate = businessDate,
                    Amount = item.Value,
                    OccurredAt = now,
                }
            );
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task ReleaseAsync(int orderId, CancellationToken cancellationToken) =>
        db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .Where(t => t.OrderId == orderId && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(t => t.OrderId, (int?)null)
                        .SetProperty(t => t.OrderLineId, (int?)null)
                        .SetProperty(t => t.ShowtimeId, (int?)null)
                        .SetProperty(t => t.ConvertedAmount, (decimal?)null),
                cancellationToken
            );

    public Task ReleaseLineAsync(
        int orderId,
        int orderLineId,
        CancellationToken cancellationToken
    ) =>
        db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .Where(t => t.OrderId == orderId && t.OrderLineId == orderLineId && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(t => t.OrderId, (int?)null)
                        .SetProperty(t => t.OrderLineId, (int?)null)
                        .SetProperty(t => t.ShowtimeId, (int?)null)
                        .SetProperty(t => t.ConvertedAmount, (decimal?)null),
                cancellationToken
            );
}
