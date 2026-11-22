using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeLifecycleService(
    IAppDbContext db,
    IBusinessEventDispatcher eventDispatcher,
    ILogger<ShowtimeLifecycleService> logger
) : IShowtimeLifecycleService
{
    public async Task<int> CompleteEndedShowtimesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var endedIds = await db
            .Showtimes.Where(s => s.Status == ShowtimeStatus.Scheduled && s.EndTime < now)
            .Select(s => s.ShowtimeId)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var showtimeId in endedIds)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                cancellationToken
            );
            var claimed = await db
                .Showtimes.Where(s =>
                    s.ShowtimeId == showtimeId && s.Status == ShowtimeStatus.Scheduled
                )
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.Status, ShowtimeStatus.Completed),
                    cancellationToken
                );
            if (claimed == 0)
                continue;

            await eventDispatcher.PublishAsync(
                new ShowtimeEnded(showtimeId, now),
                cancellationToken
            );
            await transaction.CommitAsync(cancellationToken);
            count++;
        }

        if (count > 0)
            logger.LogInformation("ShowtimesCompleted Count {Count}", count);

        return count;
    }

    public async Task<int> EndMoviesWithoutUpcomingShowtimesAsync(
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var graceDays = (
            await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken)
        ).MovieEndGraceDays;
        var cutoff = ShowtimeLifecycle.MovieEndCutoff(now, graceDays);
        var count = await db
            .Movies.Where(m =>
                m.Status == MovieStatus.NowShowing
                && m.Showtimes.Any(s => s.Status != ShowtimeStatus.Cancelled)
                && !m.Showtimes.Any(s => s.Status == ShowtimeStatus.Scheduled && s.EndTime > cutoff)
            )
            .ExecuteUpdateAsync(
                m => m.SetProperty(x => x.Status, MovieStatus.Ended),
                cancellationToken
            );
        if (count > 0)
            logger.LogInformation("MoviesEnded Count {Count}", count);

        return count;
    }
}
