namespace Cinema.Domain.Entities;

public class Bundle
{
    public int BundleId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<int> MovieIds { get; set; } = [];
    public List<int> ShowtimeIds { get; set; } = [];
    public List<int> DaysOfWeek { get; set; } = [];
    public List<int> SeatTypeIds { get; set; } = [];
    public TimeOnly? AllowedFromTime { get; set; }
    public TimeOnly? AllowedToTime { get; set; }
    public int? DailyLimit { get; set; }
    public bool ExcludesPromotions { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BundleItem> Items { get; set; } = [];
}
