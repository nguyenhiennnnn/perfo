namespace Cinema.Application.DTOs.Surveys;

public record SubmitExperienceSurveyRequest(
    int RoomRating,
    int SoundRating,
    int PictureRating,
    int CleanlinessRating,
    int CounterServiceRating
);

public record ExperienceSurveyDto(
    int SurveyId,
    int ShowtimeId,
    int OrderId,
    int RoomRating,
    int SoundRating,
    int PictureRating,
    int CleanlinessRating,
    int CounterServiceRating,
    DateTime CreatedAt
);
