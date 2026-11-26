using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class ShowtimeKindQueryExtensions
{
    public static IQueryable<Showtime> WhereRegularKind(this IQueryable<Showtime> showtimes) =>
        showtimes.Where(s => s.Kind == ShowtimeKind.Regular);

    public static IQueryable<Showtime> WherePublicKind(this IQueryable<Showtime> showtimes) =>
        showtimes.Where(s => s.Kind != ShowtimeKind.Private);
}
