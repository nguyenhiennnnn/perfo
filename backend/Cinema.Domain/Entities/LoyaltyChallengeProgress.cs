namespace Cinema.Domain.Entities;

public class LoyaltyChallengeProgress
{
    public int ProgressId { get; set; }
    public int ChallengeId { get; set; }
    public int UserId { get; set; }
    public int CurrentCount { get; set; }
    public List<int> CountedMovieIds { get; set; } = [];
    public List<int> CountedShowtimeIds { get; set; } = [];
    public DateTime? CompletedAt { get; set; }
    public DateTime? RewardGrantedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LoyaltyChallenge Challenge { get; set; } = null!;
    public User User { get; set; } = null!;
}
