namespace Cinema.Domain.Entities;

public class OrderExchange
{
    public int OrderExchangeId { get; set; }
    public int OrderId { get; set; }
    public int ExchangeNumber { get; set; }
    public int OldOrderLineId { get; set; }
    public int NewOrderLineId { get; set; }
    public int OldShowtimeId { get; set; }
    public int NewShowtimeId { get; set; }
    public decimal PriceDifference { get; set; }
    public DateTime ExchangedAt { get; set; }
    public int? PerformedByUserId { get; set; }

    public Order Order { get; set; } = null!;
    public OrderLine OldOrderLine { get; set; } = null!;
    public OrderLine NewOrderLine { get; set; } = null!;
    public Showtime OldShowtime { get; set; } = null!;
    public Showtime NewShowtime { get; set; } = null!;
    public User? PerformedByUser { get; set; }
}
