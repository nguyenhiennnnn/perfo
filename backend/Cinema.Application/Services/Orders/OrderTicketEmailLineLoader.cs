using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

internal static class OrderTicketEmailLineLoader
{
    public static async Task<IReadOnlyList<OrderTicketEmailLine>> LoadAsync(
        IAppDbContext db,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeSeatId != null
            )
            .OrderBy(l => l.ShowtimeSeat!.Seat.SeatRow)
            .ThenBy(l => l.ShowtimeSeat!.Seat.SeatNumber)
            .Select(l => new
            {
                l.ShowtimeSeat!.Seat.SeatRow,
                l.ShowtimeSeat.Seat.SeatNumber,
                TicketTypeName = l.TicketType != null ? l.TicketType.Name : null,
                l.PaidAmount,
                l.RequiresCardPresentation,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(r => new OrderTicketEmailLine(
                $"{r.SeatRow}{r.SeatNumber}",
                r.TicketTypeName ?? Messages.Common_TicketTypeStandard,
                r.PaidAmount,
                r.RequiresCardPresentation
            )),
        ];
    }
}
