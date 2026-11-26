namespace Cinema.Domain.Entities;

public class RoomRentalRate
{
    public int RoomRentalRateId { get; set; }
    public string Name { get; set; } = null!;
    public int RoomId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MinDurationMinutes { get; set; }
    public int MaxDurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PerPersonFee { get; set; }
    public int IncludedAttendees { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public CinemaRoom? Room { get; set; }
}
