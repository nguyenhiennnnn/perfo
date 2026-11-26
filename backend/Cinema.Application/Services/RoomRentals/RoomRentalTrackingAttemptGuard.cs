using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalTrackingAttemptGuard(
    IAppDbContext db,
    IAppCache cache,
    IClientContextAccessor clientContext,
    ILogger<RoomRentalTrackingAttemptGuard> logger
) : IRoomRentalTrackingAttemptGuard
{
    public const string LockedErrorCode = "ROOM_RENTAL_TRACKING_LOCKED";

    public async Task EnsureNotLockedAsync(CancellationToken cancellationToken)
    {
        var keys = CurrentKeys();
        var now = DateTime.UtcNow;
        var lockedUntil = await db
            .RoomRentalTrackingAttemptLimits.AsNoTracking()
            .Where(x => keys.Contains(x.Key) && x.LockedUntil > now)
            .MaxAsync(x => x.LockedUntil, cancellationToken);
        if (lockedUntil is not { } until)
            return;

        var remaining = until - now;
        throw new TooManyAttemptsException(
            string.Format(
                Messages.Rule_RoomRentalTrackingLocked,
                Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))
            ),
            LockedErrorCode,
            new { retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds)) }
        );
    }

    public async Task RecordFailureAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var lockFor = TimeSpan.FromMinutes(settings.RoomRentalTrackingLockoutMinutes);
        foreach (var key in CurrentKeys())
        {
            var locked = await BumpAsync(
                key,
                settings.RoomRentalTrackingMaxFailedAttempts,
                lockFor,
                cancellationToken
            );
            if (locked)
                logger.LogWarning("RoomRentalTrackingLocked Key {Key}", key);
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
                .RoomRentalTrackingAttemptLimits.Where(x => x.Key == key)
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
                return await db
                    .RoomRentalTrackingAttemptLimits.AsNoTracking()
                    .AnyAsync(x => x.Key == key && x.LockedUntil > now, cancellationToken);

            var locksImmediately = maxAttempts <= 1;
            var entity = new RoomRentalTrackingAttemptLimit
            {
                Key = key,
                FailedCount = locksImmediately ? 0 : 1,
                WindowStartedAt = now,
                LockedUntil = locksImmediately ? lockUntil : null,
            };
            db.RoomRentalTrackingAttemptLimits.Add(entity);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return locksImmediately;
            }
            catch (DbUpdateException)
            {
                db.RoomRentalTrackingAttemptLimits.Remove(entity);
            }
        }

        return false;
    }

    private List<string> CurrentKeys()
    {
        var client = clientContext.Current;
        var keys = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(client.IpAddress))
            keys.Add($"ip:{client.IpAddress}");
        if (!string.IsNullOrWhiteSpace(client.DeviceId))
            keys.Add($"device:{client.DeviceId}");
        if (keys.Count == 0)
            keys.Add("ip:unknown");
        return keys;
    }
}
