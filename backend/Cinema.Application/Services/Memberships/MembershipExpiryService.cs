using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Memberships;

public class MembershipExpiryService(IAppDbContext db, ILogger<MembershipExpiryService> logger)
    : IMembershipExpiryService
{
    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await db
            .MembershipSubscriptions.Where(s =>
                s.Status == MembershipSubscriptionStatus.Active && s.ValidTo <= now
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, MembershipSubscriptionStatus.Expired),
                cancellationToken
            );

        if (expired > 0)
            logger.LogInformation("MembershipSubscriptionsExpired Count {Count}", expired);
        return expired;
    }
}
