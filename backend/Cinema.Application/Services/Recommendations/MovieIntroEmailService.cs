using Cinema.Application.DTOs.Movies;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Recommendations;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Recommendations;

public class MovieIntroEmailService(
    IAppDbContext db,
    IMovieRecommendationService recommendations,
    INotificationService notificationService,
    IUnsubscribeTokenService unsubscribeTokens,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<MovieIntroEmailService> logger
) : IMovieIntroEmailService
{
    private const int BatchSize = 100;

    private sealed record Recipient(
        int UserId,
        string Email,
        string FullName,
        HashSet<int> NotifiedMovieIds
    );

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var newMovieIds = await LoadNewMovieIdsAsync(now, cancellationToken);
        if (newMovieIds.Count == 0)
            return 0;

        var sent = 0;
        var afterUserId = 0;
        while (true)
        {
            var page = await db
                .CustomerProfiles.AsNoTracking()
                .Where(p => p.UserId > afterUserId)
                .OrderBy(p => p.UserId)
                .Take(BatchSize)
                .Select(p => new
                {
                    Profile = p,
                    p.User.Email,
                    p.User.FullName,
                    p.User.IsActive,
                })
                .ToListAsync(cancellationToken);
            if (page.Count == 0)
                break;

            afterUserId = page[^1].Profile.UserId;
            var eligible = page.Where(x =>
                    x.IsActive
                    && !string.IsNullOrWhiteSpace(x.Email)
                    && EmailConsentRules.CanSend(x.Profile, EmailCategory.MovieRecommendation)
                )
                .ToList();
            if (eligible.Count == 0)
                continue;

            var userIds = eligible.Select(x => x.Profile.UserId).ToList();
            var notified = (
                await db
                    .MovieIntroEmailNotices.AsNoTracking()
                    .Where(n => userIds.Contains(n.UserId) && newMovieIds.Contains(n.MovieId))
                    .Select(n => new { n.UserId, n.MovieId })
                    .ToListAsync(cancellationToken)
            ).ToLookup(n => n.UserId, n => n.MovieId);

            foreach (var x in eligible)
            {
                var recipient = new Recipient(
                    x.Profile.UserId,
                    x.Email,
                    x.FullName,
                    [.. notified[x.Profile.UserId]]
                );
                if (recipient.NotifiedMovieIds.Count >= newMovieIds.Count)
                    continue;

                try
                {
                    sent += await IntroduceAsync(recipient, newMovieIds, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "MovieIntroEmailFailed UserId {UserId}", recipient.UserId);
                }
            }
        }

        if (sent > 0)
            logger.LogInformation("MovieIntroEmailsSent Count {Count}", sent);
        return sent;
    }

    private async Task<HashSet<int>> LoadNewMovieIdsAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var today = BusinessTime.Today(now);
        var cutoffDate = today.AddDays(-RecommendationWeights.NewMovieWindowDays);
        var cutoffUtc = now.AddDays(-RecommendationWeights.NewMovieWindowDays);

        var ids = await db
            .Movies.AsNoTracking()
            .Where(m =>
                m.Status == MovieStatus.NowShowing
                && (
                    (m.ReleaseDate == null && m.CreatedAt >= cutoffUtc)
                    || (m.ReleaseDate >= cutoffDate && m.ReleaseDate <= today)
                )
            )
            .Select(m => m.MovieId)
            .ToListAsync(cancellationToken);
        return [.. ids];
    }

    private async Task<int> IntroduceAsync(
        Recipient recipient,
        HashSet<int> newMovieIds,
        CancellationToken cancellationToken
    )
    {
        var result = await recommendations.GetForYouAsync(
            recipient.UserId,
            RecommendationWeights.MaxLimit,
            null,
            cancellationToken
        );
        if (!result.Personalized)
            return 0;

        var picks = result
            .Items.Where(i =>
                newMovieIds.Contains(i.Movie.MovieId)
                && !recipient.NotifiedMovieIds.Contains(i.Movie.MovieId)
                && i.Showtimes.Count > 0
                && i.ReasonType != nameof(RecommendationReasonType.Popular)
            )
            .Take(RecommendationWeights.MaxMoviesPerIntroEmail)
            .ToList();

        var claimed = new List<RecommendedMovieDto>();
        foreach (var pick in picks)
            if (await TryClaimAsync(recipient.UserId, pick.Movie.MovieId, cancellationToken))
                claimed.Add(pick);
        if (claimed.Count == 0)
            return 0;

        try
        {
            var baseUrl = appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/');
            notificationService.SendMovieIntro(
                recipient.Email,
                new MovieIntroEmailData(
                    recipient.FullName,
                    [
                        .. claimed.Select(c => new MovieIntroEmailMovieLine(
                            c.Movie.Title,
                            c.Reason,
                            $"{baseUrl}/movies/{c.Movie.MovieId}",
                            [
                                .. c.Showtimes.OrderBy(s => s.StartTime)
                                    .Select(s => new MovieIntroEmailShowtimeLine(
                                        s.StartTime,
                                        s.RoomName,
                                        s.FromPrice
                                    )),
                            ]
                        )),
                    ],
                    unsubscribeTokens.CreateUnsubscribeUrl(recipient.UserId),
                    $"{baseUrl}/account/profile"
                )
            );
        }
        catch
        {
            await ReleaseClaimsAsync(
                recipient.UserId,
                claimed.Select(c => c.Movie.MovieId).ToList(),
                CancellationToken.None
            );
            throw;
        }

        return 1;
    }

    private async Task<bool> TryClaimAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    ) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "MovieIntroEmailNotices" ("UserId", "MovieId")
            VALUES ({userId}, {movieId})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken
        ) == 1;

    private async Task ReleaseClaimsAsync(
        int userId,
        List<int> movieIds,
        CancellationToken cancellationToken
    ) =>
        await db
            .MovieIntroEmailNotices.Where(n => n.UserId == userId && movieIds.Contains(n.MovieId))
            .ExecuteDeleteAsync(cancellationToken);
}
