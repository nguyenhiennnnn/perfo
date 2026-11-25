namespace Cinema.Domain.Entities;

public class MovieSimilarity
{
    public int MovieId { get; set; }
    public int SimilarMovieId { get; set; }
    public decimal Score { get; set; }
    public int CoViewers { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Movie Movie { get; set; } = null!;
    public Movie SimilarMovie { get; set; } = null!;
}
