namespace Cinema.Domain.Entities;

public class BundleView
{
    public int BundleId { get; set; }
    public int ShowtimeId { get; set; }
    public int UserId { get; set; }
    public DateTime ViewedAt { get; set; }

    public Bundle Bundle { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public User User { get; set; } = null!;
}
