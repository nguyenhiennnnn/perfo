using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CustomerSegmentProfile
{
    public int UserId { get; set; }
    public CustomerActivityStage ActivityStage { get; set; }
    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public int? DaysSinceLastOrder { get; set; }
    public int LookbackOrderCount { get; set; }
    public decimal LookbackSpent { get; set; }
    public decimal TotalSpent { get; set; }
    public int RecencyScore { get; set; }
    public int FrequencyScore { get; set; }
    public int MonetaryScore { get; set; }
    public bool IsHighSpender { get; set; }
    public bool IsHighFrequency { get; set; }
    public decimal EstimatedLifetimeValue { get; set; }
    public int? PreferredGenreId { get; set; }
    public int? PreferredScreenTypeId { get; set; }
    public int ExpiringPoints { get; set; }
    public DateTime? PointsExpireAt { get; set; }
    public int? NextTierId { get; set; }
    public int? PointsToNextTier { get; set; }
    public bool IsNearTierUpgrade { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Genre? PreferredGenre { get; set; }
    public ScreenType? PreferredScreenType { get; set; }
    public MembershipTier? NextTier { get; set; }
}
