namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalTrackingAttemptGuard
{
    Task EnsureNotLockedAsync(CancellationToken cancellationToken);

    Task RecordFailureAsync(CancellationToken cancellationToken);
}
