namespace Cinema.Domain.Entities;

public class MovieInterest
{
    public int UserId { get; set; }
    public int MovieId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
