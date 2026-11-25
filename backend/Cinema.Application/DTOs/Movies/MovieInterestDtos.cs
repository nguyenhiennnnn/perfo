using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Movies;

public record MovieInterestDto(
    int MovieId,
    string Title,
    string? PosterUrl,
    DateOnly? ReleaseDate,
    MovieStatus Status,
    DateTime CreatedAt
);

public record RemoveMovieInterestResponse(int MovieId, bool Removed);

public record AdminMovieInterestDto(
    int MovieId,
    string Title,
    string? PosterUrl,
    DateOnly? ReleaseDate,
    MovieStatus Status,
    int InterestCount
);
