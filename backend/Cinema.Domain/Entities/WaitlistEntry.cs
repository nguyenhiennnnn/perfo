using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class WaitlistEntry
{
    public int WaitlistEntryId { get; set; }
    public int ShowtimeId { get; set; }
    public int UserId { get; set; }
    public int SeatCount { get; set; }
    public bool RequireAdjacent { get; set; }
    public int Position { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime CreatedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public User User { get; set; } = null!;
}
