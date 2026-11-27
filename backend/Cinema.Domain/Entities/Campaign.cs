using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class Campaign
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = null!;
    public int SegmentId { get; set; }
    public CampaignChannel Channel { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public PromotionDiscountType? PromoDiscountType { get; set; }
    public decimal? PromoDiscountValue { get; set; }
    public decimal? PromoMaxDiscountAmount { get; set; }
    public int? PromoValidityDays { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime? RecipientsPreparedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public ICollection<CampaignRecipient> Recipients { get; set; } = [];
}
