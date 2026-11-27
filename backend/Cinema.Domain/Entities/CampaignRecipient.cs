using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CampaignRecipient
{
    public int CampaignId { get; set; }
    public int UserId { get; set; }
    public CampaignRecipientStatus Status { get; set; } = CampaignRecipientStatus.Pending;
    public int? PromotionId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? FirstOpenedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public User User { get; set; } = null!;
    public Promotion? Promotion { get; set; }
}
