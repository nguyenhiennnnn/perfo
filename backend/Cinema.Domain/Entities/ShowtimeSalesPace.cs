namespace Cinema.Domain.Entities;

public class ShowtimeSalesPace
{
    public int ShowtimeId { get; set; }
    public int MarkMinutesBeforeStart { get; set; }
    public int TicketsSold { get; set; }
    public int SellableSeats { get; set; }
    public decimal SoldPercent { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
}
