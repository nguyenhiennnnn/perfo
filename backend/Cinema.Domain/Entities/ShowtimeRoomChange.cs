namespace Cinema.Domain.Entities;

public class ShowtimeRoomChange
{
    public int ShowtimeRoomChangeId { get; set; }
    public int ShowtimeId { get; set; }
    public int FromRoomId { get; set; }
    public int ToRoomId { get; set; }
    public int SeatsMoved { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public CinemaRoom FromRoom { get; set; } = null!;
    public CinemaRoom ToRoom { get; set; } = null!;
    public ICollection<RoomChangeRefundDue> RefundDues { get; set; } = [];
}
