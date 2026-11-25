using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Interfaces.Reviews;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reviews;

public class ReviewRewardService(IAppDbContext db, IAppCache cache, ILoyaltyPointService points)
    : IReviewRewardService
{
    public async Task<int> GrantAsync(int userId, int movieId, CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var reward = settings.ReviewRewardPoints;
        if (reward <= 0)
            return 0;

        var now = DateTime.UtcNow;
        var claimed =
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ReviewPointRewards" ("UserId", "MovieId", "Points", "GrantedAt")
                VALUES ({userId}, {movieId}, {reward}, {now})
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            ) == 1;
        if (!claimed)
            return 0;

        await points.AddLotAsync(
            userId,
            LoyaltyLotType.Bonus,
            reward,
            null,
            null,
            now,
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);
        return reward;
    }
}
