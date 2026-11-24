namespace Cinema.Domain.Entities;

public class ShowtimeRoomChangeNotice
{
    public int ShowtimeRoomChangeId { get; set; }
    public int OrderId { get; set; }
    public DateTime SentAt { get; set; }

    public ShowtimeRoomChange RoomChange { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
