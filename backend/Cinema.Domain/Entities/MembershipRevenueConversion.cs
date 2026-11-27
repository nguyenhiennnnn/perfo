namespace Cinema.Domain.Entities;

public class MembershipRevenueConversion
{
    public int MembershipRevenueConversionId { get; set; }
    public int MembershipFreeTicketId { get; set; }
    public int MembershipSubscriptionId { get; set; }
    public int MembershipPlanId { get; set; }
    public int ShowtimeId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }

    public MembershipFreeTicket MembershipFreeTicket { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
}
