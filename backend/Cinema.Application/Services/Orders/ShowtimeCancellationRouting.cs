using System.Linq.Expressions;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Orders;

internal static class ShowtimeCancellationRouting
{
    public static readonly Expression<Func<Order, bool>> CancelledByShowtime = o =>
        o.CancelReason == CancelReason.ShowtimeCancelled
        || (
            o.CancelReason == CancelReason.RoomClosed
            && (
                o.Lines.Any(l =>
                    l.LineType == OrderLineType.Ticket
                    && l.Showtime!.Status == ShowtimeStatus.Cancelled
                )
                || (
                    o.OriginalOrder != null
                    && o.OriginalOrder.Lines.Any(l =>
                        l.LineType == OrderLineType.Ticket
                        && l.Showtime!.Status == ShowtimeStatus.Cancelled
                    )
                )
            )
        );
}
