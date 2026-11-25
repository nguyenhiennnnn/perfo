using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class ShowtimeReminderService(
    IAppDbContext db,
    IAppCache cache,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<ShowtimeReminderService> logger
) : IShowtimeReminderService
{
    private const int BatchSize = 100;

    private sealed record DueTicket(
        int OrderLineId,
        int ShowtimeId,
        string QrCode,
        string SeatRow,
        int SeatNumber,
        string TicketTypeName,
        bool RequiresCardPresentation,
        string MovieTitle,
        string RoomName,
        DateTime StartTime
    );

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var windowEnd = now.AddHours(settings.ReminderHoursBefore);

        var sent = 0;
        var afterOrderId = 0;
        while (true)
        {
            var orderIds = await DueLines(now, windowEnd)
                .Where(l => l.OrderId > afterOrderId)
                .Select(l => l.OrderId)
                .Distinct()
                .OrderBy(id => id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (orderIds.Count == 0)
                break;

            afterOrderId = orderIds[^1];
            foreach (var orderId in orderIds)
            {
                try
                {
                    sent += await RemindOrderAsync(
                        orderId,
                        now,
                        windowEnd,
                        settings,
                        cancellationToken
                    );
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "ShowtimeReminderFailed OrderId {OrderId}", orderId);
                }
            }
        }

        if (sent > 0)
            logger.LogInformation("ShowtimeRemindersSent Count {Count}", sent);
        return sent;
    }

    private IQueryable<OrderLine> DueLines(DateTime now, DateTime windowEnd) =>
        db.OrderLines.Where(l =>
            l.LineType == OrderLineType.Ticket
            && l.ReminderSentAt == null
            && l.QrCode != null
            && !l.IsCheckedIn
            && l.RefundedQuantity < l.Quantity
            && db.Orders.Any(o =>
                o.OrderId == l.OrderId && o.Status == OrderStatus.Confirmed && o.UserId != null
            )
            && db.Showtimes.Any(s =>
                s.ShowtimeId == l.ShowtimeId
                && s.Status == ShowtimeStatus.Scheduled
                && s.StartTime > now
                && s.StartTime <= windowEnd
            )
            && !db.OrderExchanges.Any(e => e.OldOrderLineId == l.OrderLineId)
        );

    private async Task<int> RemindOrderAsync(
        int orderId,
        DateTime now,
        DateTime windowEnd,
        CinemaSettingsSnapshot settings,
        CancellationToken cancellationToken
    )
    {
        var recipient = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId && o.UserId != null)
            .Select(o => new
            {
                o.OrderCode,
                o.CustomerName,
                UserId = o.UserId!.Value,
                o.User!.Email,
                o.User.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
            return 0;

        if (
            !await preferences.CanSendAsync(
                recipient.UserId,
                EmailCategory.ReminderAndReviewInvite,
                cancellationToken
            )
        )
            return 0;

        var dueTickets = await DueLines(now, windowEnd)
            .Where(l => l.OrderId == orderId)
            .OrderBy(l => l.Showtime!.StartTime)
            .ThenBy(l => l.ShowtimeSeat!.Seat.SeatRow)
            .ThenBy(l => l.ShowtimeSeat!.Seat.SeatNumber)
            .Select(l => new DueTicket(
                l.OrderLineId,
                l.ShowtimeId!.Value,
                l.QrCode!,
                l.ShowtimeSeat!.Seat.SeatRow,
                l.ShowtimeSeat.Seat.SeatNumber,
                l.TicketType != null ? l.TicketType.Name : string.Empty,
                l.RequiresCardPresentation,
                l.Showtime!.Movie.Title,
                l.Showtime.Room.RoomName,
                l.Showtime.StartTime
            ))
            .ToListAsync(cancellationToken);
        if (dueTickets.Count == 0)
            return 0;

        var rootOrderId = await db.ResolveRootOrderIdAsync(orderId, cancellationToken);
        var combos = await LoadPendingCombosAsync(rootOrderId, cancellationToken);
        var ticketsLink =
            $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/account/tickets/{rootOrderId}";

        var sent = 0;
        foreach (var group in dueTickets.GroupBy(t => t.ShowtimeId))
        {
            var claimed = new List<DueTicket>();
            foreach (var ticket in group)
            {
                var updated = await DueLines(now, windowEnd)
                    .Where(l => l.OrderLineId == ticket.OrderLineId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(l => l.ReminderSentAt, now),
                        cancellationToken
                    );
                if (updated == 1)
                    claimed.Add(ticket);
            }

            if (claimed.Count == 0)
                continue;

            var first = claimed[0];
            try
            {
                notificationService.SendShowtimeReminder(
                    recipient.Email,
                    new ShowtimeReminderEmailData(
                        recipient.CustomerName ?? recipient.FullName,
                        recipient.OrderCode,
                        first.MovieTitle,
                        first.StartTime,
                        first.RoomName,
                        settings.Address,
                        settings.CheckInOpenMinutes,
                        ticketsLink,
                        [
                            .. claimed.Select(t => new ShowtimeReminderTicketLine(
                                $"{t.SeatRow}{t.SeatNumber}",
                                string.IsNullOrEmpty(t.TicketTypeName)
                                    ? Messages.Common_TicketTypeStandard
                                    : t.TicketTypeName,
                                t.QrCode,
                                t.RequiresCardPresentation
                            )),
                        ],
                        combos
                    )
                );
                sent += claimed.Count;
            }
            catch
            {
                var claimedIds = claimed.Select(t => t.OrderLineId).ToList();
                await db
                    .OrderLines.Where(l =>
                        claimedIds.Contains(l.OrderLineId) && l.ReminderSentAt != null
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(l => l.ReminderSentAt, (DateTime?)null),
                        CancellationToken.None
                    );
                throw;
            }
        }

        return sent;
    }

    private async Task<IReadOnlyList<ShowtimeReminderComboLine>> LoadPendingCombosAsync(
        int rootOrderId,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Combo
                && l.Quantity - l.RefundedQuantity - l.QuantityFulfilled > 0
                && db.Orders.Any(o =>
                    o.OrderId == l.OrderId
                    && o.Status == OrderStatus.Confirmed
                    && (
                        o.OrderId == rootOrderId
                        || (o.OriginalOrderId == rootOrderId && o.Type == OrderType.Supplement)
                    )
                )
            )
            .OrderBy(l => l.OrderId)
            .ThenBy(l => l.OrderLineId)
            .Select(l => new
            {
                l.OrderId,
                l.Order.OrderCode,
                ComboName = l.Combo!.Name,
                Remaining = l.Quantity - l.RefundedQuantity - l.QuantityFulfilled,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(r => new ShowtimeReminderComboLine(
                r.ComboName,
                r.Remaining,
                r.OrderId == rootOrderId ? null : r.OrderCode
            )),
        ];
    }
}
