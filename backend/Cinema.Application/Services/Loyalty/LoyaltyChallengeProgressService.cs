using Cinema.Application.Caching;
using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyChallengeProgressService(
    IAppDbContext db,
    IAppCache cache,
    ILoyaltyPointService points,
    LoyaltyChallengeCompletionNotifier notifier
) : ILoyaltyChallengeProgressService
{
    public async Task RecordCheckInAsync(
        TicketCheckedIn checkIn,
        CancellationToken cancellationToken
    )
    {
        var ticket = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderLineId == checkIn.OrderLineId
                && l.LineType == OrderLineType.Ticket
                && l.IsCheckedIn
                && l.RefundedQuantity < l.Quantity
                && l.Order.Status == OrderStatus.Confirmed
                && l.Order.UserId != null
                && l.Showtime != null
            )
            .Select(l => new
            {
                UserId = l.Order.UserId!.Value,
                l.Showtime!.ShowtimeId,
                l.Showtime.MovieId,
                l.Showtime.StartTime,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (ticket is null)
            return;

        var isCustomer = await db
            .Users.AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserId == ticket.UserId
                    && u.IsActive
                    && u.Role.RoleName == Roles.Customer
                    && u.CustomerProfile != null,
                cancellationToken
            );
        if (!isCustomer)
            return;

        var isPrivate = await db
            .ShowtimeDailyStats.AsNoTracking()
            .AnyAsync(
                d => d.ShowtimeId == ticket.ShowtimeId && d.Kind == ShowtimeKind.Private,
                cancellationToken
            );
        if (isPrivate)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var viewDate = BusinessDay.Of(ticket.StartTime, settings.OpeningTime, settings.ClosingTime);

        var challenges = await db
            .LoyaltyChallenges.AsNoTracking()
            .Where(c => c.IsActive && c.StartsOn <= viewDate && c.EndsOn >= viewDate)
            .OrderBy(c => c.ChallengeId)
            .ToListAsync(cancellationToken);

        foreach (var challenge in challenges)
        {
            var completion = await ApplyAsync(
                challenge,
                ticket.UserId,
                ticket.ShowtimeId,
                ticket.MovieId,
                cancellationToken
            );
            if (completion is not null)
                await notifier.NotifyAsync(completion, cancellationToken);
        }
    }

    private async Task<LoyaltyChallengeCompletion?> ApplyAsync(
        LoyaltyChallenge challenge,
        int userId,
        int showtimeId,
        int movieId,
        CancellationToken cancellationToken
    )
    {
        LoyaltyChallengeCompletion? completion = null;
        await db.InTransactionAsync(
            async () =>
            {
                await EnsureProgressAsync(challenge.ChallengeId, userId, cancellationToken);
                await LockProgressAsync(challenge.ChallengeId, userId, cancellationToken);

                var progress = await db.LoyaltyChallengeProgresses.FirstAsync(
                    p => p.ChallengeId == challenge.ChallengeId && p.UserId == userId,
                    cancellationToken
                );
                if (progress.RewardGrantedAt is not null)
                    return;

                var now = DateTime.UtcNow;
                if (progress.CompletedAt is null)
                {
                    if (progress.CountedShowtimeIds.Contains(showtimeId))
                        return;

                    progress.CountedShowtimeIds = [.. progress.CountedShowtimeIds, showtimeId];
                    if (!progress.CountedMovieIds.Contains(movieId))
                        progress.CountedMovieIds = [.. progress.CountedMovieIds, movieId];

                    var counted =
                        challenge.ConditionType == LoyaltyChallengeConditionType.DistinctMovies
                            ? progress.CountedMovieIds.Count
                            : progress.CountedShowtimeIds.Count;
                    progress.CurrentCount = Math.Min(counted, challenge.TargetCount);
                    progress.UpdatedAt = now;
                    if (counted >= challenge.TargetCount)
                        progress.CompletedAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                }

                if (progress.CompletedAt is null)
                    return;

                var claimed = await db
                    .LoyaltyChallengeProgresses.Where(p =>
                        p.ProgressId == progress.ProgressId
                        && p.CompletedAt != null
                        && p.RewardGrantedAt == null
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.RewardGrantedAt, (DateTime?)now),
                        cancellationToken
                    );
                if (claimed != 1)
                    return;

                completion = await GrantRewardAsync(challenge, userId, now, cancellationToken);
            },
            cancellationToken
        );

        return completion;
    }

    private async Task<LoyaltyChallengeCompletion> GrantRewardAsync(
        LoyaltyChallenge challenge,
        int userId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (challenge.RewardType == LoyaltyChallengeRewardType.Points)
        {
            var rewardPoints = challenge.RewardPoints!.Value;
            await points.AddLotAsync(
                userId,
                LoyaltyLotType.Bonus,
                rewardPoints,
                null,
                null,
                now,
                cancellationToken
            );
            await db.SaveChangesAsync(cancellationToken);
            return new LoyaltyChallengeCompletion(
                challenge.Name,
                userId,
                rewardPoints,
                null,
                null,
                null
            );
        }

        var reward = await db
            .LoyaltyRewards.AsNoTracking()
            .Include(r => r.Combo)
            .FirstOrDefaultAsync(r => r.RewardId == challenge.RewardGiftId, cancellationToken);
        if (reward is null || (reward.Type == LoyaltyRewardType.FreeCombo && reward.Combo is null))
            throw new BusinessRuleException(Messages.Rule_LoyaltyChallengeRewardUnavailable);

        var promotion = LoyaltyRewardPromotionFactory.Build(
            reward,
            userId,
            await LoyaltyRewardPromotionFactory.NewCodeAsync(db, cancellationToken),
            now
        );
        db.Promotions.Add(promotion);
        await db.SaveChangesAsync(cancellationToken);
        return new LoyaltyChallengeCompletion(
            challenge.Name,
            userId,
            null,
            reward.Name,
            promotion.Code,
            promotion.EndsAt
        );
    }

    private Task<int> EnsureProgressAsync(
        int challengeId,
        int userId,
        CancellationToken cancellationToken
    ) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "LoyaltyChallengeProgresses" ("ChallengeId", "UserId", "CurrentCount", "CreatedAt")
            VALUES ({challengeId}, {userId}, 0, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT ("ChallengeId", "UserId") DO NOTHING
            """,
            cancellationToken
        );

    private Task<int> LockProgressAsync(
        int challengeId,
        int userId,
        CancellationToken cancellationToken
    ) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"LoyaltyChallengeProgresses\" WHERE \"ChallengeId\" = {challengeId} AND \"UserId\" = {userId} FOR UPDATE",
            cancellationToken
        );
}
