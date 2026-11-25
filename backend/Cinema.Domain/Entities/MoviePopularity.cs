namespace Cinema.Domain.Entities;

public class MoviePopularity
{
    public int MovieId { get; set; }
    public int RecentTickets { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageRating { get; set; }
    public decimal Score { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Movie Movie { get; set; } = null!;
}
