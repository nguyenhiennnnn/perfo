using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Cinema.Infrastructure.Realtime;

public class SignalRRealtimeNotifier(IHubContext<ShowtimeHub> hubContext) : IRealtimeNotifier
{
    private static string GroupName(int showtimeId) => $"showtime-{showtimeId}";

    private static string UserGroupName(int userId) => $"user-{userId}";

    public Task NotifyShowtimeRoomChangedAsync(
        int showtimeId,
        int roomId,
        string roomName,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(
                SignalREvents.ShowtimeRoomChanged,
                new
                {
                    showtimeId,
                    roomId,
                    roomName,
                },
                cancellationToken
            );

    public Task NotifyHoldExpiredAsync(
        int showtimeId,
        int orderId,
        List<int> seatIds,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(
                SignalREvents.HoldExpired,
                new
                {
                    showtimeId,
                    bookingId = orderId,
                    seatIds,
                },
                cancellationToken
            );

    public Task NotifySeatBookedAsync(
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(SignalREvents.SeatBooked, new { showtimeId, seatIds }, cancellationToken);

    public Task NotifySeatBlockedAsync(
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(SignalREvents.SeatBlocked, new { showtimeId, seatIds }, cancellationToken);

    public Task NotifySeatHeldAsync(
        int showtimeId,
        List<int> seatIds,
        int heldByUserId,
        DateTime heldUntil,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(
                SignalREvents.SeatHeld,
                new
                {
                    showtimeId,
                    seatIds,
                    // Không gửi heldByUserId ra client để tránh lộ thông tin người dùng khác
                    heldUntil,
                },
                cancellationToken
            );

    public Task NotifySeatReleasedAsync(
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(GroupName(showtimeId))
            .SendAsync(SignalREvents.SeatReleased, new { showtimeId, seatIds }, cancellationToken);

    public Task NotifyImportExportJobProgressAsync(
        int userId,
        ImportExportJobDto job,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(UserGroupName(userId))
            .SendAsync(SignalREvents.ImportExportJobProgress, job, cancellationToken);

    public Task NotifyImportExportJobCompletedAsync(
        int userId,
        ImportExportJobDto job,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(UserGroupName(userId))
            .SendAsync(SignalREvents.ImportExportJobCompleted, job, cancellationToken);

    public Task NotifyImportExportJobFailedAsync(
        int userId,
        ImportExportJobDto job,
        CancellationToken cancellationToken
    ) =>
        hubContext
            .Clients.Group(UserGroupName(userId))
            .SendAsync(SignalREvents.ImportExportJobFailed, job, cancellationToken);
}
