namespace Cinema.Domain.Entities;

public class CustomerProfile
{
    public int UserId { get; set; }
    public int Points { get; set; }
    public int? TierId { get; set; }
    public decimal TotalSpent { get; set; }
    public bool MarketingOptIn { get; set; }
    public bool PersonalizedRecommendations { get; set; } = true;
    public bool ReminderAndReviewInvites { get; set; } = true;
    public string? ReferralCode { get; set; }

    public User User { get; set; } = null!;
    public MembershipTier? Tier { get; set; }
}
