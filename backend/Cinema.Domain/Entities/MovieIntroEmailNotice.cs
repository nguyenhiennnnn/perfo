namespace Cinema.Domain.Entities;

public class MovieIntroEmailNotice
{
    public int UserId { get; set; }
    public int MovieId { get; set; }
    public DateTime SentAt { get; set; }

    public User User { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
