using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Users;


namespace Cinema.Application.Services.Showtimes;

public class ShowtimeRoomChangedEmailHandler(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    ILogger<ShowtimeRoomChangedEmailHandler> logger
) : IBackgroundEventHandler<ShowtimeRoomChanged>
{
    public async Task HandleAsync(
        ShowtimeRoomChanged businessEvent,
        CancellationToken cancellationToken
    )
    {
        var change = await db
            .ShowtimeRoomChanges.AsNoTracking()
            .Where(c => c.ShowtimeRoomChangeId == businessEvent.ShowtimeRoomChangeId)
            .Select(c => new
            {
                c.ShowtimeId,
                MovieTitle = c.Showtime.Movie.Title,
                c.Showtime.StartTime,
                FromRoomName = c.FromRoom.RoomName,
                ToRoomName = c.ToRoom.RoomName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (change is null)
            return;

        var orders = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Confirmed
                && o.UserId != null
                && o.User!.Email != ""
                && o.Lines.Any(l =>
                    l.LineType == OrderLineType.Ticket && l.ShowtimeId == change.ShowtimeId
                )
                && !db.ShowtimeRoomChangeNotices.Any(n =>
                    n.ShowtimeRoomChangeId == businessEvent.ShowtimeRoomChangeId
                    && n.OrderId == o.OrderId
                )
            )
            .OrderBy(o => o.OrderId)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.CustomerName,
                UserId = o.UserId!.Value,
                o.User!.Email,
                o.User.FullName,
            })
            .ToListAsync(cancellationToken);

        var failed = 0;
        foreach (var order in orders)
        {
            if (!await preferences.CanSendAsync(order.UserId, EmailCategory.Service, cancellationToken))
                continue;

            if (!await TryClaimAsync(businessEvent.ShowtimeRoomChangeId, order.OrderId, cancellationToken))
                continue;

            try
            {
                var seats = await db
                    .OrderLines.AsNoTracking()
                    .Where(l =>
                        l.OrderId == order.OrderId
                        && l.LineType == OrderLineType.Ticket
                        && l.ShowtimeId == change.ShowtimeId
                        && l.ShowtimeSeatId != null
                    )
                    .OrderBy(l => l.ShowtimeSeat!.Seat.SeatRow)
                    .ThenBy(l => l.ShowtimeSeat!.Seat.SeatNumber)
                    .Select(l => l.ShowtimeSeat!.Seat.SeatRow + l.ShowtimeSeat.Seat.SeatNumber)
                    .ToListAsync(cancellationToken);

                var refundDue = await db
                    .RoomChangeRefundDues.AsNoTracking()
                    .Where(d =>
                        d.ShowtimeRoomChangeId == businessEvent.ShowtimeRoomChangeId
                        && d.OrderId == order.OrderId
                        && d.Status == RoomChangeRefundStatus.Pending
                    )
                    .SumAsync(d => (decimal?)d.AmountDue, cancellationToken);

                notificationService.SendShowtimeRoomChangedNotice(
                    order.Email,
                    order.CustomerName ?? order.FullName,
                    order.OrderCode,
                    change.MovieTitle,
                    change.StartTime,
                    change.FromRoomName,
                    change.ToRoomName,
                    seats,
                    refundDue ?? 0m
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                logger.LogError(
                    ex,
                    "ShowtimeRoomChangedEmailFailed RoomChangeId {RoomChangeId} OrderId {OrderId}",
                    businessEvent.ShowtimeRoomChangeId,
                    order.OrderId
                );
                await ReleaseClaimAsync(
                    businessEvent.ShowtimeRoomChangeId,
                    order.OrderId,
                    CancellationToken.None
                );
            }
            catch (OperationCanceledException)
            {
                await ReleaseClaimAsync(
                    businessEvent.ShowtimeRoomChangeId,
                    order.OrderId,
                    CancellationToken.None
                );
                throw;
            }
        }

        if (failed > 0)
            throw new InvalidOperationException(
                $"ShowtimeRoomChangedEmail failed for {failed} order(s), RoomChangeId {businessEvent.ShowtimeRoomChangeId}"
            );
    }

    private async Task<bool> TryClaimAsync(
        int roomChangeId,
        int orderId,
        CancellationToken cancellationToken
    ) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ShowtimeRoomChangeNotices" ("ShowtimeRoomChangeId", "OrderId")
            VALUES ({roomChangeId}, {orderId})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken
        ) == 1;

    private async Task ReleaseClaimAsync(
        int roomChangeId,
        int orderId,
        CancellationToken cancellationToken
    ) =>
        await db
            .ShowtimeRoomChangeNotices.Where(n =>
                n.ShowtimeRoomChangeId == roomChangeId && n.OrderId == orderId
            )
            .ExecuteDeleteAsync(cancellationToken);
}
