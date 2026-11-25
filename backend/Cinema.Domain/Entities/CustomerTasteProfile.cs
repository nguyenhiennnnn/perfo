namespace Cinema.Domain.Entities;

public class CustomerTasteProfile
{
    public int UserId { get; set; }
    public int MovieCount { get; set; }
    public decimal? TypicalTicketPrice { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<CustomerTasteSignal> Signals { get; set; } = [];
}
