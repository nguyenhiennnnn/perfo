using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class Showtime
{
    public int ShowtimeId { get; set; }
    public int MovieId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public ShowtimeStatus Status { get; set; } = ShowtimeStatus.Scheduled;
    public ShowtimeKind Kind { get; set; } = ShowtimeKind.Regular;
    public int? RoomRentalRequestId { get; set; }
    public string? ContentTitle { get; set; }
    public DateTime CreatedAt { get; set; }

    public Movie Movie { get; set; } = null!;
    public CinemaRoom Room { get; set; } = null!;
    public RoomRentalRequest? RoomRentalRequest { get; set; }
    public ShowtimeEventSetting? EventSetting { get; set; }
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = [];
    public ICollection<OrderLine> OrderLines { get; set; } = [];
}
