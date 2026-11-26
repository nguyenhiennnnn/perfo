namespace Cinema.Domain.Entities;

public class RoomRentalTrackingAttemptLimit
{
    public string Key { get; set; } = null!;
    public int FailedCount { get; set; }
    public DateTime WindowStartedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
}
