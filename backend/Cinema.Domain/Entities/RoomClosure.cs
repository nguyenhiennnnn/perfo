namespace Cinema.Domain.Entities;

public class RoomClosure
{
    public int RoomClosureId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Reason { get; set; } = null!;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public CinemaRoom Room { get; set; } = null!;
}
