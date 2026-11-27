using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public PromotionType Type { get; set; }
    public PromotionDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public PromotionScope Scope { get; set; } = PromotionScope.WholeOrder;
    public decimal? MinOrderAmount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public SalesChannel? Channel { get; set; }
    public List<int> MovieIds { get; set; } = [];
    public List<int> ShowtimeIds { get; set; } = [];
    public List<int> ScreenTypeIds { get; set; } = [];
    public List<int> DaysOfWeek { get; set; } = [];
    public List<int> MembershipTierIds { get; set; } = [];
    public bool NewCustomersOnly { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int? PerCustomerLimit { get; set; }
    public decimal? BudgetAmount { get; set; }
    public int ConsumedCount { get; set; }
    public decimal ConsumedBudget { get; set; }
    public int Priority { get; set; }
    public bool IsExclusive { get; set; }
    public bool ExcludeEventShowtimes { get; set; }
    public int? AssignedUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? AssignedUser { get; set; }
    public ICollection<PromotionRedemption> Redemptions { get; set; } = [];
}
