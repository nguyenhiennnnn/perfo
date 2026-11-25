using Cinema.Application.Caching;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyChallengeQueryService(IAppDbContext db, IAppCache cache)
    : ILoyaltyChallengeQueryService
{
    public async Task<IReadOnlyList<MyLoyaltyChallengeDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var today = BusinessDay.Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime);

        var rows = await db
            .LoyaltyChallenges.AsNoTracking()
            .Where(c => c.IsActive && c.EndsOn >= today)
            .OrderBy(c => c.StartsOn)
            .ThenBy(c => c.ChallengeId)
            .Select(c => new
            {
                Challenge = c,
                GiftName = c.RewardGift != null ? c.RewardGift.Name : null,
                Progress = c
                    .Progresses.Where(p => p.UserId == userId)
                    .Select(p => new
                    {
                        p.CurrentCount,
                        p.CompletedAt,
                        p.RewardGrantedAt,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new MyLoyaltyChallengeDto(
                r.Challenge.ChallengeId,
                r.Challenge.Name,
                r.Challenge.Description,
                r.Challenge.StartsOn,
                r.Challenge.EndsOn,
                r.Challenge.ConditionType.ToString(),
                r.Challenge.TargetCount,
                r.Challenge.RewardType.ToString(),
                r.Challenge.RewardPoints,
                r.GiftName,
                r.Progress?.CurrentCount ?? 0,
                r.Progress?.CompletedAt is not null,
                r.Progress?.CompletedAt,
                r.Progress?.RewardGrantedAt is not null,
                r.Challenge.StartsOn <= today
            ))
            .ToList();
    }
}
