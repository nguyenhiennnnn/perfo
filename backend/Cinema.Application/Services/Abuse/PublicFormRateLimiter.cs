using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Abuse;

public class PublicFormRateLimiter(
    IAppDbContext db,
    IAppCache cache,
    IClientContextAccessor clientContext,
    ILogger<PublicFormRateLimiter> logger
) : IPublicFormRateLimiter
{
    public const string RateLimitedErrorCode = "PUBLIC_FORM_RATE_LIMITED";

    public async Task EnsureAllowedAsync(string form, CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var window = TimeSpan.FromMinutes(settings.PublicFormWindowMinutes);
        var now = DateTime.UtcNow;

        DateTime? blockedUntil = null;
        foreach (var key in Keys(form))
        {
            var (count, windowStartedAt) = await HitAsync(key, now, window, cancellationToken);
            if (count <= settings.PublicFormMaxRequests)
                continue;

            var until = windowStartedAt + window;
            if (blockedUntil is null || until > blockedUntil)
                blockedUntil = until;
            logger.LogWarning("PublicFormRateLimited Key {Key} Count {Count}", key, count);
        }

        if (blockedUntil is not { } retryAt)
            return;

        var remaining = retryAt - now;
        throw new TooManyAttemptsException(
            string.Format(
                Messages.Rule_PublicFormRateLimited,
                Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))
            ),
            RateLimitedErrorCode,
            new { retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds)) }
        );
    }

    private async Task<(int Count, DateTime WindowStartedAt)> HitAsync(
        string key,
        DateTime now,
        TimeSpan window,
        CancellationToken cancellationToken
    )
    {
        var windowStart = now - window;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var updated = await db
                .PublicFormAttemptLimits.Where(x => x.Key == key)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.Count,
                                x => x.WindowStartedAt < windowStart ? 1 : x.Count + 1
                            )
                            .SetProperty(
                                x => x.WindowStartedAt,
                                x => x.WindowStartedAt < windowStart ? now : x.WindowStartedAt
                            ),
                    cancellationToken
                );
            if (updated > 0)
            {
                var row = await db
                    .PublicFormAttemptLimits.AsNoTracking()
                    .Where(x => x.Key == key)
                    .Select(x => new { x.Count, x.WindowStartedAt })
                    .FirstAsync(cancellationToken);
                return (row.Count, row.WindowStartedAt);
            }

            var entity = new PublicFormAttemptLimit
            {
                Key = key,
                Count = 1,
                WindowStartedAt = now,
            };
            db.PublicFormAttemptLimits.Add(entity);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return (1, now);
            }
            catch (DbUpdateException)
            {
                db.PublicFormAttemptLimits.Remove(entity);
            }
        }

        return (1, now);
    }

    private List<string> Keys(string form)
    {
        var client = clientContext.Current;
        var keys = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(client.IpAddress))
            keys.Add($"ip:{form}:{client.IpAddress}");
        if (!string.IsNullOrWhiteSpace(client.DeviceId))
            keys.Add($"device:{form}:{client.DeviceId}");
        if (keys.Count == 0)
            keys.Add($"ip:{form}:unknown");
        return keys;
    }
}
