using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class SeatHoldLimitService(IAppDbContext db, IAppCache cache) : ISeatHoldLimitService
{
    public const string PendingOrdersLimitErrorCode = "PENDING_ORDERS_LIMIT_EXCEEDED";
    public const string HeldSeatsLimitErrorCode = "HELD_SEATS_LIMIT_EXCEEDED";

    private const int LockClassId = 18;

    public async Task<int> EnsureWithinLimitsAsync(
        int userId,
        int seatCount,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {userId})",
            cancellationToken
        );

        var streak = await ExpiredStreakQuery(userId).CountAsync(cancellationToken);
        DateTime? restrictedUntil = null;
        if (streak >= settings.ExpiredOrderStreakThreshold)
        {
            var lastExpiredAt = await ExpiredStreakQuery(userId)
                .MaxAsync(o => o.ExpiresAt, cancellationToken);
            if (lastExpiredAt is { } last)
            {
                var until = last.AddMinutes(settings.ExpiredOrderRestrictionMinutes);
                if (until > now)
                    restrictedUntil = until;
            }
        }
        var restricted = restrictedUntil is not null;

        var pending = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.UserId == userId
                && o.Channel == SalesChannel.Online
                && o.Type == OrderType.Ticket
                && o.Status == OrderStatus.Pending
                && o.ExpiresAt > now
            )
            .Select(o => new
            {
                o.ExpiresAt,
                Seats = o.Lines.Count(l => l.LineType == OrderLineType.Ticket),
            })
            .ToListAsync(cancellationToken);

        var maxPending = restricted
            ? Math.Min(settings.MaxPendingOrdersPerUser, settings.RestrictedMaxPendingOrders)
            : settings.MaxPendingOrdersPerUser;
        if (pending.Count >= maxPending)
        {
            var earliest = pending.Min(p => p.ExpiresAt!.Value);
            var message = restricted
                ? string.Format(
                    Messages.Rule_HoldRestrictedPendingLimit,
                    streak,
                    maxPending,
                    Format(restrictedUntil!.Value),
                    Format(earliest)
                )
                : string.Format(
                    Messages.Rule_PendingOrdersLimit,
                    maxPending,
                    Format(earliest)
                );
            throw new TooManyAttemptsException(
                message,
                PendingOrdersLimitErrorCode,
                new { retryAfterSeconds = RetryAfterSeconds(earliest, now) }
            );
        }

        var heldSeats = pending.Sum(p => p.Seats);
        if (heldSeats + seatCount > settings.MaxHeldSeatsPerUser)
        {
            var earliest = pending.Count > 0 ? pending.Min(p => p.ExpiresAt!.Value) : now;
            throw new TooManyAttemptsException(
                string.Format(
                    Messages.Rule_HeldSeatsLimit,
                    heldSeats,
                    settings.MaxHeldSeatsPerUser,
                    seatCount,
                    Format(earliest)
                ),
                HeldSeatsLimitErrorCode,
                new { retryAfterSeconds = RetryAfterSeconds(earliest, now) }
            );
        }

        return restricted
            ? Math.Min(settings.RestrictedHoldMinutes, settings.HoldDurationMinutes)
            : settings.HoldDurationMinutes;
    }

    private IQueryable<Order> ExpiredStreakQuery(int userId) =>
        db.Orders.AsNoTracking()
            .Where(o =>
                o.UserId == userId
                && o.Channel == SalesChannel.Online
                && o.Type == OrderType.Ticket
                && o.Status == OrderStatus.Expired
                && !db.Orders.Any(c =>
                    c.UserId == userId
                    && c.Type == OrderType.Ticket
                    && c.Status == OrderStatus.Confirmed
                    && c.CreatedAt >= o.CreatedAt
                )
            );

    private static int RetryAfterSeconds(DateTime until, DateTime now) =>
        Math.Max(1, (int)Math.Ceiling((until - now).TotalSeconds));

    private static string Format(DateTime utc) =>
        BusinessTime.ToLocal(utc).ToString(DateTimePatterns.DateHourMinute);
}
