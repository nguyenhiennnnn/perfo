using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardAttemptGuard(
    IAppDbContext db,
    IAppCache cache,
    ICurrentActorAccessor actorAccessor,
    ILogger<GiftCardAttemptGuard> logger
) : IGiftCardAttemptGuard
{
    public const string LockedErrorCode = "GIFT_CARD_ATTEMPTS_LOCKED";

    public async Task EnsureNotLockedAsync(CancellationToken cancellationToken)
    {
        var keys = CurrentKeys();
        if (keys.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var lockedUntil = await db
            .GiftCardAttemptLimits.AsNoTracking()
            .Where(x => keys.Contains(x.Key) && x.LockedUntil > now)
            .MaxAsync(x => x.LockedUntil, cancellationToken);
        if (lockedUntil is not { } until)
            return;

        var remaining = until - now;
        throw new TooManyAttemptsException(
            string.Format(
                Messages.Rule_GiftCardAttemptsLocked,
                Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))
            ),
            LockedErrorCode,
            new { retryAfterSeconds = (int)Math.Ceiling(remaining.TotalSeconds) }
        );
    }

    public async Task RecordFailureAsync(CancellationToken cancellationToken)
    {
        var keys = CurrentKeys();
        if (keys.Count == 0)
            return;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var lockFor = TimeSpan.FromMinutes(settings.GiftCardLockoutMinutes);
        foreach (var key in keys)
        {
            var locked = await BumpAsync(
                key,
                settings.GiftCardMaxFailedAttempts,
                lockFor,
                cancellationToken
            );
            if (locked)
                logger.LogWarning("GiftCardAttemptsLocked Key {Key}", key);
        }
    }

    private async Task<bool> BumpAsync(
        string key,
        int maxAttempts,
        TimeSpan lockFor,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var windowStart = now - lockFor;
        var lockUntil = (DateTime?)(now + lockFor);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var updated = await db
                .GiftCardAttemptLimits.Where(x => x.Key == key)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.LockedUntil,
                                x =>
                                    (x.WindowStartedAt < windowStart ? 1 : x.FailedCount + 1)
                                    >= maxAttempts
                                        ? lockUntil
                                        : x.LockedUntil
                            )
                            .SetProperty(
                                x => x.FailedCount,
                                x =>
                                    (x.WindowStartedAt < windowStart ? 1 : x.FailedCount + 1)
                                    >= maxAttempts
                                        ? 0
                                        : (x.WindowStartedAt < windowStart ? 1 : x.FailedCount + 1)
                            )
                            .SetProperty(
                                x => x.WindowStartedAt,
                                x => x.WindowStartedAt < windowStart ? now : x.WindowStartedAt
                            ),
                    cancellationToken
                );
            if (updated > 0)
                return await IsLockedAsync(key, now, cancellationToken);

            var locksImmediately = maxAttempts <= 1;
            var entity = new GiftCardAttemptLimit
            {
                Key = key,
                FailedCount = locksImmediately ? 0 : 1,
                WindowStartedAt = now,
                LockedUntil = locksImmediately ? lockUntil : null,
            };
            db.GiftCardAttemptLimits.Add(entity);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return locksImmediately;
            }
            catch (DbUpdateException)
            {
                db.GiftCardAttemptLimits.Remove(entity);
            }
        }

        return false;
    }

    private Task<bool> IsLockedAsync(
        string key,
        DateTime now,
        CancellationToken cancellationToken
    ) =>
        db
            .GiftCardAttemptLimits.AsNoTracking()
            .AnyAsync(x => x.Key == key && x.LockedUntil > now, cancellationToken);

    private List<string> CurrentKeys()
    {
        var keys = new List<string>(2);
        var actor = actorAccessor.Current;
        if (actor is null)
            return keys;

        keys.Add($"user:{actor.UserId}");
        if (!string.IsNullOrWhiteSpace(actor.IpAddress))
            keys.Add($"ip:{actor.IpAddress}");
        return keys;
    }
}
