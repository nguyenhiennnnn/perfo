namespace Cinema.Domain.Entities;

public class MembershipFreeTicket
{
    public int MembershipFreeTicketId { get; set; }
    public int MembershipSubscriptionId { get; set; }
    public int UserId { get; set; }
    public int PeriodIndex { get; set; }
    public int Sequence { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? OrderId { get; set; }
    public int? OrderLineId { get; set; }

    public MembershipSubscription MembershipSubscription { get; set; } = null!;
    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public OrderLine? OrderLine { get; set; }
}
