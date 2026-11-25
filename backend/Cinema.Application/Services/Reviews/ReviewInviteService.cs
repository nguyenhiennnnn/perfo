using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Reviews;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Reviews;

public class ReviewInviteService(
    IAppDbContext db,
    IAppCache cache,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<ReviewInviteService> logger
) : IReviewInviteService
{
    private const int BatchSize = 200;

    private sealed record DueTicket(
        int OrderLineId,
        int UserId,
        int MovieId,
        int ShowtimeId,
        string MovieTitle,
        string Email,
        string FullName,
        bool IsActive
    );

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var endedAfter = now.AddHours(-settings.ReviewInviteMaxHours);
        var endedBefore = now.AddHours(-settings.ReviewInviteMinHours);
        var baseUrl = appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/');

        var handled = new HashSet<(int UserId, int MovieId)>();
        var sent = 0;
        var afterLineId = 0;
        while (true)
        {
            var page = await DueLines(endedAfter, endedBefore)
                .Where(l => l.OrderLineId > afterLineId)
                .OrderBy(l => l.OrderLineId)
                .Take(BatchSize)
                .Select(l => new DueTicket(
                    l.OrderLineId,
                    l.Order.UserId!.Value,
                    l.Showtime!.MovieId,
                    l.ShowtimeId!.Value,
                    l.Showtime.Movie.Title,
                    l.Order.User!.Email,
                    l.Order.User.FullName,
                    l.Order.User.IsActive
                ))
                .ToListAsync(cancellationToken);
            if (page.Count == 0)
                break;

            afterLineId = page[^1].OrderLineId;
            foreach (var ticket in page)
            {
                if (!handled.Add((ticket.UserId, ticket.MovieId)))
                    continue;

                try
                {
                    sent += await InviteAsync(ticket, settings.ReviewRewardPoints, baseUrl, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "ReviewInviteFailed UserId {UserId} MovieId {MovieId}",
                        ticket.UserId,
                        ticket.MovieId
                    );
                }
            }
        }

        if (sent > 0)
            logger.LogInformation("ReviewInvitesSent Count {Count}", sent);
        return sent;
    }

    private IQueryable<Domain.Entities.OrderLine> DueLines(DateTime endedAfter, DateTime endedBefore) =>
        db.OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.IsCheckedIn
                && l.RefundedQuantity < l.Quantity
                && l.Order.Status == OrderStatus.Confirmed
                && l.Order.UserId != null
                && db.Showtimes.Any(s =>
                    s.ShowtimeId == l.ShowtimeId
                    && s.Status != ShowtimeStatus.Cancelled
                    && s.EndTime > endedAfter
                    && s.EndTime <= endedBefore
                    && !db.ShowtimeDailyStats.Any(d =>
                        d.ShowtimeId == s.ShowtimeId && d.Kind != ShowtimeKind.Regular
                    )
                )
                && !db.Reviews.Any(r =>
                    r.UserId == l.Order.UserId && r.MovieId == l.Showtime!.MovieId
                )
                && !db.ReviewInviteNotices.Any(n =>
                    n.UserId == l.Order.UserId && n.MovieId == l.Showtime!.MovieId
                )
            );

    private async Task<int> InviteAsync(
        DueTicket ticket,
        int rewardPoints,
        string baseUrl,
        CancellationToken cancellationToken
    )
    {
        if (!ticket.IsActive || string.IsNullOrWhiteSpace(ticket.Email))
            return 0;

        if (
            !await preferences.CanSendAsync(
                ticket.UserId,
                EmailCategory.ReminderAndReviewInvite,
                cancellationToken
            )
        )
            return 0;

        if (!await TryClaimAsync(ticket, cancellationToken))
            return 0;

        try
        {
            notificationService.SendReviewInvite(
                ticket.Email,
                new ReviewInviteEmailData(
                    ticket.FullName,
                    ticket.MovieTitle,
                    $"{baseUrl}/movies/{ticket.MovieId}?survey={ticket.ShowtimeId}#reviews",
                    rewardPoints
                )
            );
        }
        catch
        {
            await db
                .ReviewInviteNotices.Where(n =>
                    n.UserId == ticket.UserId && n.MovieId == ticket.MovieId
                )
                .ExecuteDeleteAsync(CancellationToken.None);
            throw;
        }

        return 1;
    }

    private async Task<bool> TryClaimAsync(DueTicket ticket, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ReviewInviteNotices" ("UserId", "MovieId", "ShowtimeId", "SentAt")
            VALUES ({ticket.UserId}, {ticket.MovieId}, {ticket.ShowtimeId}, {DateTime.UtcNow})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken
        ) == 1;
}
