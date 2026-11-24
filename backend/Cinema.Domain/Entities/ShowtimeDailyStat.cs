using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ShowtimeDailyStat
{
    public int ShowtimeId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public ShowtimeKind Kind { get; set; } = ShowtimeKind.Regular;
    public int SellableSeats { get; set; }
    public int TicketsSold { get; set; }
    public decimal GrossTicketRevenue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal ComboRevenue { get; set; }
    public int CancelledOrders { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
}
