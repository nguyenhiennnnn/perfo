namespace Cinema.Domain.Entities;

public class ExperienceSurvey
{
    public int SurveyId { get; set; }
    public int UserId { get; set; }
    public int ShowtimeId { get; set; }
    public int OrderId { get; set; }
    public int RoomRating { get; set; }
    public int SoundRating { get; set; }
    public int PictureRating { get; set; }
    public int CleanlinessRating { get; set; }
    public int CounterServiceRating { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
