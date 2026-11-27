using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ShowtimeEventSetting
{
    public int ShowtimeId { get; set; }
    public PricingAdjustmentMode AdjustmentMode { get; set; }
    public decimal AdjustmentValue { get; set; }
    public int? MaxTicketsPerCustomer { get; set; }
    public DateTime? GeneralSaleStartTime { get; set; }
    public List<int> EarlyAccessTierIds { get; set; } = [];
    public bool EarlyAccessPaidMembers { get; set; }
    public bool EarlyAccessMovieInterest { get; set; }
    public bool AllowMembershipFreeTickets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public ICollection<ShowtimeEventTicketQuota> TicketQuotas { get; set; } = [];
    public ICollection<ShowtimeEventGift> Gifts { get; set; } = [];
}
