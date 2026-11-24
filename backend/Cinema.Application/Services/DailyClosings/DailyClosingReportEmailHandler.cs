using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.DailyClosings;

public class DailyClosingReportEmailHandler(
    IAppDbContext db,
    IDailyClosingLedgerReportService reports,
    INotificationService notifications,
    ILogger<DailyClosingReportEmailHandler> logger
) : IBackgroundEventHandler<DailyClosed>
{
    public async Task HandleAsync(DailyClosed businessEvent, CancellationToken cancellationToken)
    {
        var closing = await db
            .DailyClosings.AsNoTracking()
            .Where(c =>
                c.DailyClosingId == businessEvent.DailyClosingId
                && c.Status == DailyClosingStatus.Closed
            )
            .Select(c => new
            {
                c.BusinessDate,
                c.ExpectedCash,
                c.CountedCash,
                c.Variance,
                c.Note,
                c.ClosedAt,
                c.ReportEmailedAt,
                ClosedByName = c.ClosedByUser!.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (closing is null || closing.ReportEmailedAt is not null)
            return;

        var claimedAt = DateTime.UtcNow;
        var claimed = await db
            .DailyClosings.Where(c =>
                c.DailyClosingId == businessEvent.DailyClosingId && c.ReportEmailedAt == null
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.ReportEmailedAt, (DateTime?)claimedAt),
                cancellationToken
            );
        if (claimed == 0)
            return;

        var sent = 0;
        try
        {
            var report = await reports.GetAsync(
                closing.BusinessDate,
                DailyClosingReportMode.PostingDate,
                cancellationToken
            );
            sent = await SendAsync(
                new DailyClosingEmailData(
                    closing.BusinessDate,
                    closing.ClosedByName,
                    closing.ClosedAt ?? businessEvent.OccurredAt,
                    closing.Note,
                    report.Adjusted.TicketRevenue,
                    report.Adjusted.ComboRevenue,
                    report.Adjusted.TotalRevenue,
                    report.Adjusted.GiftCardSold,
                    report.Adjusted.GiftCardUsed,
                    report.Adjusted.TotalDiscount,
                    report.Adjusted.TicketRefundAmount,
                    report.Adjusted.CancelledOrders,
                    report.AdjustmentTotals.Count,
                    report.AdjustmentTotals.TicketRevenue + report.AdjustmentTotals.ComboRevenue,
                    closing.ExpectedCash ?? 0m,
                    closing.CountedCash ?? 0m,
                    closing.Variance ?? 0m,
                    report.Snapshot.Orders.PendingOrders,
                    report.Snapshot.Orders.ProblemOrders
                ),
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            await ReleaseClaimAsync(businessEvent.DailyClosingId, claimedAt);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "DailyClosingReportEmailFailed DailyClosingId {DailyClosingId}",
                businessEvent.DailyClosingId
            );
            await ReleaseClaimAsync(businessEvent.DailyClosingId, claimedAt);
            throw;
        }

        if (sent == 0)
        {
            await ReleaseClaimAsync(businessEvent.DailyClosingId, claimedAt);
            throw new InvalidOperationException(
                $"DailyClosingReportEmail not delivered, DailyClosingId {businessEvent.DailyClosingId}"
            );
        }

        logger.LogInformation(
            "DailyClosingReportEmailSent DailyClosingId {DailyClosingId} Recipients {Count}",
            businessEvent.DailyClosingId,
            sent
        );
    }

    private async Task<int> SendAsync(DailyClosingEmailData data, CancellationToken cancellationToken)
    {
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
            logger.LogWarning("DailyClosingReportEmailNoRecipients");
            return 0;
        }

        var sent = 0;
        foreach (var email in recipients)
        {
            try
            {
                notifications.SendDailyClosingReport(email, data);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DailyClosingReportEmailEnqueueFailed");
            }
        }

        return sent;
    }

    private async Task ReleaseClaimAsync(int dailyClosingId, DateTime claimedAt) =>
        await db
            .DailyClosings.Where(c =>
                c.DailyClosingId == dailyClosingId && c.ReportEmailedAt == claimedAt
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.ReportEmailedAt, (DateTime?)null),
                CancellationToken.None
            );
}
