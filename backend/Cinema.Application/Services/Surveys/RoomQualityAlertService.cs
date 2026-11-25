using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Surveys;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Surveys;

public class RoomQualityAlertService(
    IAppDbContext db,
    IAppCache cache,
    INotificationService notifications,
    ILogger<RoomQualityAlertService> logger
) : IRoomQualityAlertService
{
    private sealed record DayScore(decimal Sum, int Count)
    {
        public decimal Average => Sum / Count;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var days = Math.Max(settings.RoomQualityAlertDays, 1);
        var threshold = settings.RoomQualityAlertThreshold;
        var now = DateTime.UtcNow;
        var today = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);

        var byRoom = await LoadDayScoresAsync(
            today,
            days,
            settings.OpeningTime,
            settings.ClosingTime,
            cancellationToken
        );

        await ResolveRecoveredAsync(byRoom, threshold, now, cancellationToken);
        await DetectAsync(byRoom, today, days, threshold, now, cancellationToken);
        return await NotifyPendingAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Dictionary<DateOnly, DayScore>>> LoadDayScoresAsync(
        DateOnly today,
        int days,
        TimeOnly? opening,
        TimeOnly? closing,
        CancellationToken cancellationToken
    )
    {
        var fromUtc = BusinessDay.StartUtc(today.AddDays(-days), opening, closing);
        var toUtc = BusinessDay.EndUtc(today, opening, closing);

        var rows = await db
            .ExperienceSurveys.AsNoTracking()
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
            .Select(s => new
            {
                s.Showtime.RoomId,
                s.CreatedAt,
                s.RoomRating,
                s.SoundRating,
                s.PictureRating,
                s.CleanlinessRating,
            })
            .ToListAsync(cancellationToken);

        return rows.GroupBy(r => r.RoomId)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.GroupBy(r => BusinessDay.Of(r.CreatedAt, opening, closing))
                        .ToDictionary(
                            d => d.Key,
                            d =>
                                new DayScore(
                                    d.Sum(r =>
                                        (
                                            r.RoomRating
                                            + r.SoundRating
                                            + r.PictureRating
                                            + r.CleanlinessRating
                                        ) / 4m
                                    ),
                                    d.Count()
                                )
                        )
            );
    }

    private async Task ResolveRecoveredAsync(
        Dictionary<int, Dictionary<DateOnly, DayScore>> byRoom,
        decimal threshold,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var active = await db
            .RoomQualityAlerts.AsNoTracking()
            .Where(a => a.ResolvedAt == null)
            .Select(a => new { a.RoomQualityAlertId, a.RoomId })
            .ToListAsync(cancellationToken);

        foreach (var alert in active)
        {
            var recovered =
                !byRoom.TryGetValue(alert.RoomId, out var scores)
                || scores[scores.Keys.Max()].Average >= threshold;
            if (!recovered)
                continue;

            await db
                .RoomQualityAlerts.Where(a =>
                    a.RoomQualityAlertId == alert.RoomQualityAlertId && a.ResolvedAt == null
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.ResolvedAt, (DateTime?)now),
                    cancellationToken
                );
        }
    }

    private async Task DetectAsync(
        Dictionary<int, Dictionary<DateOnly, DayScore>> byRoom,
        DateOnly today,
        int days,
        decimal threshold,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var qualified = new List<(int RoomId, DateOnly From, DateOnly To, decimal Average)>();
        foreach (var (roomId, scores) in byRoom)
        {
            var end = scores.ContainsKey(today) ? today : today.AddDays(-1);
            var streak = Enumerable
                .Range(0, days)
                .Select(i => end.AddDays(-i))
                .Select(d => scores.GetValueOrDefault(d))
                .ToList();
            if (streak.Any(s => s is null || s.Average >= threshold))
                continue;

            var average = Math.Round(
                streak.Sum(s => s!.Sum) / streak.Sum(s => s!.Count),
                2,
                MidpointRounding.AwayFromZero
            );
            qualified.Add((roomId, end.AddDays(-(days - 1)), end, average));
        }
        if (qualified.Count == 0)
            return;

        var candidateIds = qualified.Select(q => q.RoomId).ToList();
        var eligible = (
            await db
                .CinemaRooms.AsNoTracking()
                .Where(r => candidateIds.Contains(r.RoomId) && r.IsActive)
                .Select(r => r.RoomId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();
        var alreadyActive = (
            await db
                .RoomQualityAlerts.AsNoTracking()
                .Where(a => a.ResolvedAt == null && candidateIds.Contains(a.RoomId))
                .Select(a => a.RoomId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        foreach (var q in qualified.Where(q => eligible.Contains(q.RoomId) && !alreadyActive.Contains(q.RoomId)))
        {
            var alert = new RoomQualityAlert
            {
                RoomId = q.RoomId,
                FromDate = q.From,
                ToDate = q.To,
                ConsecutiveDays = days,
                AverageScore = q.Average,
                Threshold = threshold,
                DetectedAt = now,
            };
            db.RoomQualityAlerts.Add(alert);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.RoomQualityAlerts.Remove(alert);
                if (
                    !await db.RoomQualityAlerts.AnyAsync(
                        a => a.RoomId == q.RoomId && a.ResolvedAt == null,
                        CancellationToken.None
                    )
                )
                    throw;
            }
        }
    }

    private async Task<int> NotifyPendingAsync(CancellationToken cancellationToken)
    {
        var pendingIds = await db
            .RoomQualityAlerts.AsNoTracking()
            .Where(a => a.ResolvedAt == null && a.NotifiedAt == null)
            .OrderBy(a => a.RoomQualityAlertId)
            .Select(a => a.RoomQualityAlertId)
            .ToListAsync(cancellationToken);
        if (pendingIds.Count == 0)
            return 0;

        var claimedAt = DateTime.UtcNow;
        var claimedIds = new List<int>();
        foreach (var alertId in pendingIds)
        {
            var claimed = await db
                .RoomQualityAlerts.Where(a =>
                    a.RoomQualityAlertId == alertId && a.ResolvedAt == null && a.NotifiedAt == null
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.NotifiedAt, (DateTime?)claimedAt),
                    cancellationToken
                );
            if (claimed == 1)
                claimedIds.Add(alertId);
        }

        if (claimedIds.Count == 0)
            return 0;

        var delivered = false;
        try
        {
            delivered = await SendAsync(claimedIds, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RoomQualityAlertSendFailed Alerts {Count}", claimedIds.Count);
        }
        catch (OperationCanceledException)
        {
            await ReleaseClaimAsync(claimedIds, claimedAt);
            throw;
        }

        if (!delivered)
        {
            await ReleaseClaimAsync(claimedIds, claimedAt);
            return 0;
        }

        logger.LogInformation("RoomQualityAlertSent Alerts {Count}", claimedIds.Count);
        return claimedIds.Count;
    }

    private async Task<bool> SendAsync(List<int> alertIds, CancellationToken cancellationToken)
    {
        var lines = await db
            .RoomQualityAlerts.AsNoTracking()
            .Where(a => alertIds.Contains(a.RoomQualityAlertId))
            .OrderBy(a => a.Room.RoomName)
            .ThenBy(a => a.RoomQualityAlertId)
            .Select(a => new RoomQualityEmailLine(
                a.Room.RoomName,
                a.AverageScore,
                a.ConsecutiveDays,
                a.Threshold,
                a.FromDate,
                a.ToDate
            ))
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            return false;

        var recipients = await db
            .Users.AsNoTracking()
            .Where(u =>
                u.IsActive && u.Role.RoleName == Roles.Admin && u.Email != null && u.Email != ""
            )
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            logger.LogWarning("RoomQualityAlertNoRecipients Alerts {Count}", lines.Count);
            return false;
        }

        var sent = 0;
        foreach (var email in recipients)
        {
            try
            {
                notifications.SendRoomQualityAlert(email, lines);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "RoomQualityAlertEnqueueFailed");
            }
        }

        return sent > 0;
    }

    private async Task ReleaseClaimAsync(List<int> alertIds, DateTime claimedAt)
    {
        await db
            .RoomQualityAlerts.Where(a =>
                alertIds.Contains(a.RoomQualityAlertId) && a.NotifiedAt == claimedAt
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.NotifiedAt, (DateTime?)null),
                CancellationToken.None
            );
    }
}
