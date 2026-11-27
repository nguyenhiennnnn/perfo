using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class MembershipSubscription
{
    public int MembershipSubscriptionId { get; set; }
    public int UserId { get; set; }
    public int MembershipPlanId { get; set; }
    public int OrderId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public MembershipSubscriptionStatus Status { get; set; } = MembershipSubscriptionStatus.Active;
    public DateTime? ExpiryReminderSentAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public MembershipRefundReason? RefundReason { get; set; }
    public decimal StoppedRevenueAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public MembershipPlan MembershipPlan { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
