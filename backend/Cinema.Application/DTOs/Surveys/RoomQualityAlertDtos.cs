namespace Cinema.Application.DTOs.Surveys;

public record RoomQualityAlertDto(
    int RoomQualityAlertId,
    int RoomId,
    string RoomName,
    DateOnly FromDate,
    DateOnly ToDate,
    int ConsecutiveDays,
    decimal AverageScore,
    decimal Threshold,
    DateTime DetectedAt,
    DateTime? NotifiedAt,
    DateTime? ResolvedAt
);
