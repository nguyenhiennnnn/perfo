namespace Cinema.Domain.Entities;

public class ReviewPointReward
{
    public int UserId { get; set; }
    public int MovieId { get; set; }
    public int Points { get; set; }
    public DateTime GrantedAt { get; set; }

    public User User { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
