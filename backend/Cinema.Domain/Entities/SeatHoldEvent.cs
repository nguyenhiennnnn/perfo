using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class SeatHoldEvent
{
    public long Id { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public int RoomId { get; set; }
    public int MovieId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public SeatHoldEventType Type { get; set; }
    public DateTime OccurredAt { get; set; }
}
