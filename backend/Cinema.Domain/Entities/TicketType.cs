namespace Cinema.Domain.Entities;

public class TicketType
{
    public int TicketTypeId { get; set; }
    public string Name { get; set; } = null!;
    public decimal PriceMultiplier { get; set; } = 1m;
    public bool RequiresCardVerification { get; set; }
    public List<int> AllowedAgeRatingIds { get; set; } = [];
    public TimeOnly? AllowedFromTime { get; set; }
    public TimeOnly? AllowedToTime { get; set; }
    public List<int> AllowedDaysOfWeek { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
