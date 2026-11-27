using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Showtimes;

public static class ShowtimeEventLimitRules
{
    public const int LockClassId = 25;
    public const int MaxLimit = 100_000;

    public static IQueryable<OrderLine> ActiveTickets(
        this IAppDbContext db,
        int showtimeId,
        DateTime now
    ) =>
        db.OrderLines.Where(l =>
            l.ShowtimeId == showtimeId
            && l.LineType == OrderLineType.Ticket
            && l.Quantity > l.RefundedQuantity
            && (
                l.Order.Status == OrderStatus.Confirmed
                || (
                    l.Order.Status == OrderStatus.Pending
                    && (l.Order.ExpiresAt == null || l.Order.ExpiresAt > now)
                )
            )
        );
}
