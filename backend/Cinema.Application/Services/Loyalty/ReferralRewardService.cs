using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class ReferralRewardService(
    IAppDbContext db,
    IAppCache cache,
    ILoyaltyPointService points,
    IReferralDeviceCheck deviceCheck
) : IReferralRewardService
{
    public async Task RewardForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var referredUserId = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.OrderId == orderId
                && o.Status == OrderStatus.Confirmed
                && o.UserId != null
                && (o.Type == OrderType.Ticket || o.Type == OrderType.Combo)
            )
            .Select(o => o.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (referredUserId is not { } referredId)
            return;

        var relation = await db
            .ReferralRelations.AsNoTracking()
            .Where(r =>
                r.ReferredUserId == referredId
                && r.RewardGrantedAt == null
                && r.RewardBlockedAt == null
            )
            .Select(r => new
            {
                r.ReferralId,
                r.ReferrerUserId,
                ReferredEmail = r.Referred.Email,
                ReferredPhone = r.Referred.Phone,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (relation is null)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        await db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM \"CustomerProfiles\" WHERE \"UserId\" = {relation.ReferrerUserId} FOR UPDATE",
                    cancellationToken
                );

                var now = DateTime.UtcNow;
                var reason = await FindBlockReasonAsync(
                    relation.ReferrerUserId,
                    referredId,
                    relation.ReferredEmail,
                    relation.ReferredPhone,
                    settings,
                    now,
                    cancellationToken
                );

                if (reason is { } blocked)
                {
                    await db
                        .ReferralRelations.Where(r =>
                            r.ReferralId == relation.ReferralId
                            && r.RewardGrantedAt == null
                            && r.RewardBlockedAt == null
                        )
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(r => r.RewardBlockedAt, (DateTime?)now)
                                    .SetProperty(
                                        r => r.RewardBlockedReason,
                                        (ReferralBlockReason?)blocked
                                    ),
                            cancellationToken
                        );
                    return;
                }

                var claimed = await db
                    .ReferralRelations.Where(r =>
                        r.ReferralId == relation.ReferralId
                        && r.RewardGrantedAt == null
                        && r.RewardBlockedAt == null
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(r => r.RewardGrantedAt, (DateTime?)now),
                        cancellationToken
                    );
                if (claimed != 1)
                    return;

                if (settings.ReferralReferrerPoints > 0)
                    await points.AddLotAsync(
                        relation.ReferrerUserId,
                        LoyaltyLotType.Bonus,
                        settings.ReferralReferrerPoints,
                        null,
                        null,
                        now,
                        cancellationToken
                    );
                if (settings.ReferralReferredPoints > 0)
                    await points.AddLotAsync(
                        referredId,
                        LoyaltyLotType.Bonus,
                        settings.ReferralReferredPoints,
                        null,
                        null,
                        now,
                        cancellationToken
                    );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    private async Task<ReferralBlockReason?> FindBlockReasonAsync(
        int referrerUserId,
        int referredUserId,
        string referredEmail,
        string? referredPhone,
        CinemaSettingsSnapshot settings,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var phones = ReferralIdentityKeys.PhoneVariants(referredPhone);
        if (
            phones.Count > 0
            && await db.Users.AsNoTracking()
                .AnyAsync(
                    u => u.UserId != referredUserId && u.Phone != null && phones.Contains(u.Phone),
                    cancellationToken
                )
        )
            return ReferralBlockReason.DuplicatePhone;

        var (plain, prefix, suffix) = ReferralIdentityKeys.EmailKeys(referredEmail);
        if (
            await db.Users.AsNoTracking()
                .AnyAsync(
                    u =>
                        u.UserId != referredUserId
                        && (
                            u.Email.ToLower() == plain
                            || (u.Email.ToLower().StartsWith(prefix) && u.Email.ToLower().EndsWith(suffix))
                        ),
                    cancellationToken
                )
        )
            return ReferralBlockReason.DuplicateEmail;

        if (await deviceCheck.IsSuspiciousAsync(referrerUserId, referredUserId, cancellationToken))
            return ReferralBlockReason.DeviceFlagged;

        var today = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        var monthStart = BusinessDay.StartUtc(
            new DateOnly(today.Year, today.Month, 1),
            settings.OpeningTime,
            settings.ClosingTime
        );
        var rewardedThisMonth = await db
            .ReferralRelations.AsNoTracking()
            .CountAsync(
                r => r.ReferrerUserId == referrerUserId && r.RewardGrantedAt >= monthStart,
                cancellationToken
            );
        return rewardedThisMonth >= settings.ReferralMonthlyRewardLimit
            ? ReferralBlockReason.MonthlyLimitReached
            : null;
    }
}
