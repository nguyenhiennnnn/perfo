namespace Cinema.Domain.Entities;

public class SeatType
{
    public int SeatTypeId { get; set; }
    public string Name { get; set; } = null!;
    public decimal PriceMultiplier { get; set; } = 1.0m;
    public bool IsCouple { get; set; }

    public ICollection<Seat> Seats { get; set; } = [];
}
