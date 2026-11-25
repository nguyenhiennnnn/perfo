using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Recommendations;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Recommendations;

public class CustomerTasteProfileService(IAppDbContext db) : ICustomerTasteProfileService
{
    public async Task RebuildForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var userId = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => o.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is { } id)
            await RebuildAsync(id, cancellationToken);
    }

    public async Task RebuildForOrderLineAsync(int orderLineId, CancellationToken cancellationToken)
    {
        var userId = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderLineId == orderLineId)
            .Select(l => l.Order.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is { } id)
            await RebuildAsync(id, cancellationToken);
    }

    public Task RebuildAsync(int userId, CancellationToken cancellationToken) =>
        db.InTransactionAsync(
            async () =>
            {
                await LockCustomerAsync(userId, cancellationToken);

                var optedIn = await db
                    .CustomerProfiles.AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .Select(p => (bool?)p.PersonalizedRecommendations)
                    .FirstOrDefaultAsync(cancellationToken);
                if (optedIn is not true)
                {
                    await DeleteAsync(userId, cancellationToken);
                    return;
                }

                var now = DateTime.UtcNow;
                var result = await ComputeAsync(userId, now, cancellationToken);
                await ReplaceAsync(userId, result, now, cancellationToken);
            },
            cancellationToken
        );

    public Task ClearAsync(int userId, CancellationToken cancellationToken) =>
        db.InTransactionAsync(
            async () =>
            {
                await LockCustomerAsync(userId, cancellationToken);
                await DeleteAsync(userId, cancellationToken);
            },
            cancellationToken
        );

    private async Task<TasteResult> ComputeAsync(
        int userId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var lines = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Order.UserId == userId
                && l.Order.Status == OrderStatus.Confirmed
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeId != null
                && l.Showtime!.Status != ShowtimeStatus.Cancelled
                && !db.ShowtimeDailyStats.Any(d =>
                    d.ShowtimeId == l.ShowtimeId && d.Kind == ShowtimeKind.Private
                )
            )
            .Select(l => new
            {
                ShowtimeId = l.ShowtimeId!.Value,
                l.Showtime!.MovieId,
                l.Showtime.StartTime,
                l.Showtime.Room.ScreenTypeId,
                l.IsCheckedIn,
                l.CheckedInAt,
                l.LockedUnitPrice,
            })
            .ToListAsync(cancellationToken);

        var viewings = lines
            .GroupBy(l => l.ShowtimeId)
            .Select(g =>
            {
                var first = g.First();
                var checkedIn = g.Any(l => l.IsCheckedIn);
                return new TasteViewing(
                    g.Key,
                    first.MovieId,
                    first.StartTime,
                    first.ScreenTypeId,
                    checkedIn,
                    checkedIn ? g.Where(l => l.IsCheckedIn).Max(l => l.CheckedInAt) : null,
                    g.Average(l => l.LockedUnitPrice)
                );
            })
            .ToList();

        var reviews = await db
            .Reviews.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new TasteReview(r.MovieId, r.Rating, r.CreatedAt))
            .ToListAsync(cancellationToken);

        var movieIds = viewings.Select(v => v.MovieId).Union(reviews.Select(r => r.MovieId)).ToList();
        var movies = await LoadMoviesAsync(movieIds, cancellationToken);

        return TasteProfileCalculator.Calculate(viewings, reviews, movies, now);
    }

    private async Task<Dictionary<int, TasteMovie>> LoadMoviesAsync(
        List<int> movieIds,
        CancellationToken cancellationToken
    )
    {
        if (movieIds.Count == 0)
            return [];

        var directors = await db
            .Movies.AsNoTracking()
            .Where(m => movieIds.Contains(m.MovieId))
            .Select(m => new { m.MovieId, m.DirectorId })
            .ToListAsync(cancellationToken);
        var genres = (
            await db
                .MovieGenres.AsNoTracking()
                .Where(g => movieIds.Contains(g.MovieId))
                .Select(g => new { g.MovieId, g.GenreId })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(g => g.MovieId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GenreId).ToList());
        var actors = (
            await db
                .MovieCast.AsNoTracking()
                .Where(c => movieIds.Contains(c.MovieId))
                .Select(c => new { c.MovieId, c.ActorId })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(c => c.MovieId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ActorId).ToList());

        return directors.ToDictionary(
            d => d.MovieId,
            d => new TasteMovie(
                d.MovieId,
                d.DirectorId,
                genres.GetValueOrDefault(d.MovieId) ?? [],
                actors.GetValueOrDefault(d.MovieId) ?? []
            )
        );
    }

    private async Task ReplaceAsync(
        int userId,
        TasteResult result,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        await db
            .CustomerTasteSignals.Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var profile = await db.CustomerTasteProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
        {
            profile = new CustomerTasteProfile { UserId = userId };
            db.CustomerTasteProfiles.Add(profile);
        }

        profile.MovieCount = result.MovieCount;
        profile.TypicalTicketPrice = result.TypicalTicketPrice;
        profile.UpdatedAt = now;

        db.CustomerTasteSignals.AddRange(
            result.Signals.Select(s => new CustomerTasteSignal
            {
                UserId = userId,
                Kind = s.Kind,
                RefId = s.RefId,
                Weight = s.Weight,
            })
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<int> DeleteAsync(int userId, CancellationToken cancellationToken) =>
        db.CustomerTasteProfiles.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    private Task<int> LockCustomerAsync(int userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"CustomerProfiles\" WHERE \"UserId\" = {userId} FOR UPDATE",
            cancellationToken
        );
}
