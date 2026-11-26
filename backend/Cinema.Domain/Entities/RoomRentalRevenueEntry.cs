namespace Cinema.Domain.Entities;

public class RoomRentalRevenueEntry
{
    public int RoomRentalRevenueEntryId { get; set; }
    public int RoomRentalRequestId { get; set; }
    public int ShowtimeId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }

    public RoomRentalRequest Request { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
}
