using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Recommendations;

public static class CheckedInViewingQueries
{
    public static IQueryable<OrderLine> CheckedInViewings(
        this IQueryable<OrderLine> lines,
        IAppDbContext db
    ) => lines.ActiveViewings(db).Where(l => l.IsCheckedIn);

    public static IQueryable<OrderLine> WatchedViewings(
        this IQueryable<OrderLine> lines,
        IAppDbContext db,
        DateTime utcNow
    ) => lines.ActiveViewings(db).Where(l => l.IsCheckedIn || l.Showtime!.EndTime <= utcNow);

    private static IQueryable<OrderLine> ActiveViewings(
        this IQueryable<OrderLine> lines,
        IAppDbContext db
    ) =>
        lines
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Order.Status == OrderStatus.Confirmed
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeId != null
                && l.Showtime!.Status != ShowtimeStatus.Cancelled
                && !db.ShowtimeDailyStats.Any(d =>
                    d.ShowtimeId == l.ShowtimeId && d.Kind == ShowtimeKind.Private
                )
            );
}
