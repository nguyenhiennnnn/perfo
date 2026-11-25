using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Movies;

namespace Cinema.Application.Interfaces.Movies;

public interface IMovieInterestService
{
    Task<List<MovieInterestDto>> GetMineAsync(int userId, CancellationToken cancellationToken);

    Task<MovieInterestDto> AddAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    );

    Task<RemoveMovieInterestResponse> RemoveAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<AdminMovieInterestDto>> GetCountsAdminAsync(
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<List<int>> GetInterestedUserIdsAsync(int movieId, CancellationToken cancellationToken);

    Task<bool> IsInterestedAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    );
}
