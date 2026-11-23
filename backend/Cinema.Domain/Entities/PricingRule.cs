using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class PricingRule
{
    public int PricingRuleId { get; set; }
    public string Name { get; set; } = null!;
    public PricingRuleType RuleType { get; set; }
    public string ConditionJson { get; set; } = "{}";
    public PricingAdjustmentMode AdjustmentMode { get; set; }
    public decimal? AdjustmentValue { get; set; }
    public int Priority { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public List<int> RoomIds { get; set; } = [];
    public List<int> ScreenTypeIds { get; set; } = [];
    public List<int> MovieIds { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
