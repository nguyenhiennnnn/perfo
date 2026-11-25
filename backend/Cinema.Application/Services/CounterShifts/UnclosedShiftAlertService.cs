using Cinema.Application.Caching;
using Cinema.Application.Interfaces.CounterShifts;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.CounterShifts;

public class UnclosedShiftAlertService(
    IAppDbContext db,
    IAppCache cache,
    INotificationService notifications,
    ILogger<UnclosedShiftAlertService> logger
) : IUnclosedShiftAlertService
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var overdueBefore = BusinessDay.CurrentStartUtc(
            DateTime.UtcNow,
            settings.OpeningTime,
            settings.ClosingTime
        );

        var candidateIds = await db
            .CounterShifts.AsNoTracking()
            .Where(s =>
                s.Status == CounterShiftStatus.Open
                && s.UnclosedAlertedAt == null
                && s.OpenedAt < overdueBefore
            )
            .OrderBy(s => s.CounterShiftId)
            .Select(s => s.CounterShiftId)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return 0;

        var claimedAt = DateTime.UtcNow;
        var claimedIds = new List<int>();
        foreach (var shiftId in candidateIds)
        {
            var claimed = await db
                .CounterShifts.Where(s =>
                    s.CounterShiftId == shiftId
                    && s.Status == CounterShiftStatus.Open
                    && s.UnclosedAlertedAt == null
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(s => s.UnclosedAlertedAt, (DateTime?)claimedAt),
                    cancellationToken
                );
            if (claimed == 1)
                claimedIds.Add(shiftId);
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
            logger.LogError(ex, "UnclosedShiftAlertSendFailed Shifts {Count}", claimedIds.Count);
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

        logger.LogInformation("UnclosedShiftAlertSent Shifts {Count}", claimedIds.Count);
        return claimedIds.Count;
    }

    private async Task<bool> SendAsync(List<int> shiftIds, CancellationToken cancellationToken)
    {
        var lines = await db
            .CounterShifts.AsNoTracking()
            .Where(s => shiftIds.Contains(s.CounterShiftId))
            .OrderBy(s => s.OpenedAt)
            .ThenBy(s => s.CounterShiftId)
            .Select(s => new UnclosedShiftEmailLine(
                s.CounterShiftId,
                s.Employee.User.FullName,
                s.OpenedAt,
                s.OpeningCash
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
            logger.LogWarning("UnclosedShiftAlertNoRecipients Shifts {Count}", lines.Count);
            return false;
        }

        var sent = 0;
        foreach (var email in recipients)
        {
            try
            {
                notifications.SendUnclosedShiftAlert(email, lines);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "UnclosedShiftAlertEnqueueFailed");
            }
        }

        return sent > 0;
    }

    private async Task ReleaseClaimAsync(List<int> shiftIds, DateTime claimedAt)
    {
        await db
            .CounterShifts.Where(s =>
                shiftIds.Contains(s.CounterShiftId) && s.UnclosedAlertedAt == claimedAt
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.UnclosedAlertedAt, (DateTime?)null),
                CancellationToken.None
            );
    }
}
