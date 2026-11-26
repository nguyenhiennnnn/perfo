using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class MembershipPlan
{
    public int MembershipPlanId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public MembershipPlanPeriod Period { get; set; }
    public decimal Price { get; set; }
    public decimal TicketDiscountPercent { get; set; }
    public int FreeTicketsPerMonth { get; set; }
    public decimal ComboDiscountPercent { get; set; }
    public bool EarlyAccess { get; set; }
    public bool StackableWithPromotions { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MembershipSubscription> Subscriptions { get; set; } = [];
}
