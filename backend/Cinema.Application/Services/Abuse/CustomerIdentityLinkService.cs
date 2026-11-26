using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Abuse;

public class CustomerIdentityLinkService(IAppDbContext db) : ICustomerIdentityLinkService
{
    public Task<bool> SharesDeviceWithOtherAsync(
        int userId,
        CancellationToken cancellationToken
    ) =>
        db.CustomerDevices.AsNoTracking()
            .AnyAsync(
                d =>
                    d.UserId != userId
                    && db.CustomerDevices.Any(m => m.UserId == userId && m.DeviceId == d.DeviceId),
                cancellationToken
            );

    public async Task<IReadOnlyList<int>> FindLinkedUserIdsAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var phone = await db
            .Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.Phone)
            .FirstOrDefaultAsync(cancellationToken);
        var phones = ReferralIdentityKeys.PhoneVariants(phone);

        var byPhone = new List<int>();
        if (phones.Count > 0)
            byPhone = await db
                .Users.AsNoTracking()
                .Where(u => u.UserId != userId && u.Phone != null && phones.Contains(u.Phone))
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

        var byDevice = await db
            .CustomerDevices.AsNoTracking()
            .Where(d =>
                d.UserId != userId
                && db.CustomerDevices.Any(m => m.UserId == userId && m.DeviceId == d.DeviceId)
            )
            .Select(d => d.UserId)
            .ToListAsync(cancellationToken);

        return byPhone.Union(byDevice).ToList();
    }
}
