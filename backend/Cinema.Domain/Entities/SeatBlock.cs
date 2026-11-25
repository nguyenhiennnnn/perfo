using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class SeatBlock
{
    public int SeatBlockId { get; set; }
    public int ShowtimeId { get; set; }
    public int ShowtimeSeatId { get; set; }
    public SeatBlockType Type { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? WaitlistEntryId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public ShowtimeSeat ShowtimeSeat { get; set; } = null!;
    public WaitlistEntry? WaitlistEntry { get; set; }
}
