using Cinema.Application.DTOs.Movies;

namespace Cinema.Application.Interfaces.Recommendations;

public interface IMovieRecommendationService
{
    Task<MovieRecommendationsDto> GetForYouAsync(
        int? userId,
        int? limit,
        CancellationToken cancellationToken
    );
}
