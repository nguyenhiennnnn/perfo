namespace Cinema.Domain.Entities;

public class MembershipRevenueEntry
{
    public int MembershipRevenueEntryId { get; set; }
    public int MembershipSubscriptionId { get; set; }
    public int MonthIndex { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }

    public MembershipSubscription MembershipSubscription { get; set; } = null!;
}
