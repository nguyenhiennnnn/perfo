using Cinema.Application.Interfaces.Bundles;
using Cinema.Application.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class BundleViewTracker(IAppDbContext db) : IBundleViewTracker
{
    public async Task RecordAsync(
        int userId,
        int showtimeId,
        IReadOnlyCollection<int> bundleIds,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        foreach (var bundleId in bundleIds.Distinct())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"BundleViews\" (\"BundleId\", \"ShowtimeId\", \"UserId\", \"ViewedAt\") VALUES ({bundleId}, {showtimeId}, {userId}, {now}) ON CONFLICT (\"BundleId\", \"ShowtimeId\", \"UserId\") DO NOTHING",
                cancellationToken
            );
        }
    }
}
