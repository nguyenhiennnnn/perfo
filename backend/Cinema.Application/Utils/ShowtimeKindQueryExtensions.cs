using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class ShowtimeKindQueryExtensions
{
    public static IQueryable<Showtime> WhereRegularKind(
        this IQueryable<Showtime> showtimes,
        IAppDbContext db
    ) =>
        showtimes.Where(s =>
            !db.ShowtimeDailyStats.Any(d =>
                d.ShowtimeId == s.ShowtimeId && d.Kind != ShowtimeKind.Regular
            )
        );
}
