namespace Cinema.Domain.Entities;

public class RoomQualityAlert
{
    public int RoomQualityAlertId { get; set; }
    public int RoomId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int ConsecutiveDays { get; set; }
    public decimal AverageScore { get; set; }
    public decimal Threshold { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public CinemaRoom Room { get; set; } = null!;
}
