using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Abuse;

public class AbuseSignalService(
    IAppDbContext db,
    IAppCache cache,
    ILogger<AbuseSignalService> logger
) : IAbuseSignalService
{
    private const int LockClassId = 19;

    public Task RecordHoldAbandonedAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    ) => RecordAsync(userId, orderId, RiskSignalType.HoldAbandoned, cancellationToken);

    public Task RecordPaymentFailedAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    ) => RecordAsync(userId, orderId, RiskSignalType.PaymentFailed, cancellationToken);

    private async Task RecordAsync(
        int userId,
        int orderId,
        RiskSignalType type,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var (threshold, windowMinutes) = type switch
        {
            RiskSignalType.HoldAbandoned => (
                settings.AbuseHoldAbandonThreshold,
                settings.AbuseHoldAbandonWindowMinutes
            ),
            _ => (
                settings.AbusePaymentFailureThreshold,
                settings.AbusePaymentFailureWindowMinutes
            ),
        };
        var now = DateTime.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {userId})",
            cancellationToken
        );

        db.RiskSignals.Add(
            new RiskSignal
            {
                UserId = userId,
                Type = type,
                OrderId = orderId,
                CreatedAt = now,
            }
        );
        await db.SaveChangesAsync(cancellationToken);

        var since = now.AddMinutes(-windowMinutes);
        var lastVerifiedAt = await db
            .CustomerVerifications.AsNoTracking()
            .Where(v => v.UserId == userId && v.Status == CustomerVerificationStatus.Verified)
            .MaxAsync(v => v.VerifiedAt, cancellationToken);
        if (lastVerifiedAt is { } verifiedAt && verifiedAt > since)
            since = verifiedAt;

        var count = await db
            .RiskSignals.AsNoTracking()
            .CountAsync(
                s => s.UserId == userId && s.Type == type && s.CreatedAt > since,
                cancellationToken
            );
        if (count < threshold)
            return;

        var hasPending = await db
            .CustomerVerifications.AsNoTracking()
            .AnyAsync(
                v => v.UserId == userId && v.Status == CustomerVerificationStatus.Pending,
                cancellationToken
            );
        if (hasPending)
            return;

        db.CustomerVerifications.Add(
            new CustomerVerification
            {
                UserId = userId,
                Reason = type,
                Status = CustomerVerificationStatus.Pending,
                CreatedAt = now,
            }
        );
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "VerificationRequired UserId {UserId} Reason {Reason} Signals {Count}",
            userId,
            type,
            count
        );
    }
}
