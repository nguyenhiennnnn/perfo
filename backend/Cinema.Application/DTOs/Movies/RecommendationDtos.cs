namespace Cinema.Application.DTOs.Movies;

public record RecommendedMovieDto(
    MovieListItemDto Movie,
    string Reason,
    string ReasonType,
    List<UpcomingShowtimeDto> Showtimes
);

public record MovieRecommendationsDto(bool Personalized, List<RecommendedMovieDto> Items);

public record RecommendationWindow(DateOnly? Date, TimeOnly? FromTime);
