using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipSubscriptionQueryService(IAppDbContext db)
    : IMembershipSubscriptionQueryService
{
    public async Task<IReadOnlyList<MembershipSubscriptionDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var rows = await db
            .MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ValidFrom)
            .Take(Pagination.MaxPageSize)
            .Select(s => new
            {
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                PlanName = s.MembershipPlan.Name,
                s.OrderId,
                s.ValidFrom,
                s.ValidTo,
                s.Status,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(s => new MembershipSubscriptionDto(
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                s.PlanName,
                s.OrderId,
                s.ValidFrom,
                s.ValidTo,
                s.Status == MembershipSubscriptionStatus.Active
                    && s.ValidFrom <= now
                    && s.ValidTo > now,
                s.Status.ToString()
            ))
            .ToList();
    }
}
