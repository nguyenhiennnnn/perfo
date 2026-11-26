using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Movies;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Movies;

public class MovieInterestNotificationService(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<MovieInterestNotificationService> logger
) : IMovieInterestNotificationService
{
    private const int BatchSize = 100;

    public async Task<int> NotifyOpenedAsync(int movieId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var title = await db
            .Movies.AsNoTracking()
            .Where(m => m.MovieId == movieId && m.Status != MovieStatus.Ended)
            .Select(m => m.Title)
            .FirstOrDefaultAsync(cancellationToken);
        if (title is null)
            return 0;

        var firstOpenedAt = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.MovieId == movieId)
            .MinAsync(s => (DateTime?)s.CreatedAt, cancellationToken);
        var firstStartTime = await db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.MovieId == movieId && s.Status == ShowtimeStatus.Scheduled && s.StartTime > now
            )
            .MinAsync(s => (DateTime?)s.StartTime, cancellationToken);
        if (firstOpenedAt is null || firstStartTime is null)
            return 0;

        var movieLink = $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/movies/{movieId}";
        var sent = 0;
        var failures = new List<Exception>();
        var afterUserId = 0;

        while (true)
        {
            var page = await db
                .MovieInterests.AsNoTracking()
                .Where(i =>
                    i.MovieId == movieId
                    && i.NotifiedAt == null
                    && i.CreatedAt <= firstOpenedAt
                    && i.UserId > afterUserId
                )
                .OrderBy(i => i.UserId)
                .Take(BatchSize)
                .Select(i => new
                {
                    i.UserId,
                    i.User.Email,
                    i.User.FullName,
                    i.User.IsActive,
                })
                .ToListAsync(cancellationToken);
            if (page.Count == 0)
                break;

            afterUserId = page[^1].UserId;
            foreach (var recipient in page)
            {
                if (
                    !recipient.IsActive
                    || string.IsNullOrWhiteSpace(recipient.Email)
                    || !await preferences.CanSendAsync(
                        recipient.UserId,
                        EmailCategory.Service,
                        cancellationToken
                    )
                )
                    continue;

                var claimed = await db
                    .MovieInterests.Where(i =>
                        i.UserId == recipient.UserId
                        && i.MovieId == movieId
                        && i.NotifiedAt == null
                    )
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.NotifiedAt, now), cancellationToken);
                if (claimed == 0)
                    continue;

                try
                {
                    notificationService.SendMovieOpenedForSale(
                        recipient.Email,
                        new MovieOpenedForSaleEmailData(
                            recipient.FullName,
                            title,
                            firstStartTime.Value,
                            movieLink
                        )
                    );
                    sent++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "MovieOpenedNoticeFailed MovieId {MovieId} UserId {UserId}",
                        movieId,
                        recipient.UserId
                    );
                    await db
                        .MovieInterests.Where(i =>
                            i.UserId == recipient.UserId && i.MovieId == movieId
                        )
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(i => i.NotifiedAt, (DateTime?)null),
                            CancellationToken.None
                        );
                    failures.Add(ex);
                }
            }
        }

        if (sent > 0)
            logger.LogInformation(
                "MovieOpenedNotified MovieId {MovieId} Count {Count}",
                movieId,
                sent
            );
        if (failures.Count > 0)
            throw new AggregateException(failures);
        return sent;
    }
}
