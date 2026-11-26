using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipFreeTicketQueryService(IAppDbContext db) : IMembershipFreeTicketQueryService
{
    public async Task<MembershipFreeTicketBalanceDto> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var periods = await db
            .MembershipFreeTickets.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.PeriodStart <= now && t.PeriodEnd > now)
            .GroupBy(t => new
            {
                t.MembershipSubscriptionId,
                PlanName = t.MembershipSubscription.MembershipPlan.Name,
                t.PeriodStart,
                t.PeriodEnd,
            })
            .Select(g => new
            {
                g.Key.MembershipSubscriptionId,
                g.Key.PlanName,
                g.Key.PeriodStart,
                g.Key.PeriodEnd,
                Granted = g.Count(),
                Used = g.Count(t => t.UsedAt != null),
            })
            .OrderBy(p => p.PeriodEnd)
            .ToListAsync(cancellationToken);

        var items = periods
            .Select(p => new MembershipFreeTicketPeriodDto(
                p.MembershipSubscriptionId,
                p.PlanName,
                p.PeriodStart,
                p.PeriodEnd,
                p.Granted,
                p.Used,
                p.Granted - p.Used
            ))
            .ToList();
        return new MembershipFreeTicketBalanceDto(items.Sum(i => i.Remaining), items);
    }
}
