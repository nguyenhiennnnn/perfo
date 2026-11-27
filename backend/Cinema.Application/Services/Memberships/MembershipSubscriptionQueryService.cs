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
        return await db
            .MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ValidFrom)
            .Take(Pagination.MaxPageSize)
            .Select(s => new MembershipSubscriptionDto(
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                s.MembershipPlan.Name,
                s.OrderId,
                s.ValidFrom,
                s.ValidTo,
                s.Status == MembershipSubscriptionStatus.Active && s.ValidFrom <= now && s.ValidTo > now
            ))
            .ToListAsync(cancellationToken);
    }
}
