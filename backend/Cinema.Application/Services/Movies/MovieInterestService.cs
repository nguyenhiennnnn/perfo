using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Movies;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Movies;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Movies;

public class MovieInterestService(IAppDbContext db) : IMovieInterestService
{
    private const int MaxMineItems = 200;

    public async Task<List<MovieInterestDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    ) =>
        await ProjectDtos(db.MovieInterests.AsNoTracking().Where(i => i.UserId == userId))
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.MovieId)
            .Take(MaxMineItems)
            .ToListAsync(cancellationToken);

    public async Task<MovieInterestDto> AddAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    )
    {
        var status =
            await db
                .Movies.AsNoTracking()
                .Where(m => m.MovieId == movieId)
                .Select(m => (MovieStatus?)m.Status)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Movie);

        if (await FindAsync(userId, movieId, cancellationToken) is { } existing)
            return existing;

        if (status != MovieStatus.ComingSoon)
            throw new BusinessRuleException(Messages.Rule_MovieInterestNotComingSoon);

        var interest = new MovieInterest
        {
            UserId = userId,
            MovieId = movieId,
            CreatedAt = DateTime.UtcNow,
        };
        db.MovieInterests.Add(interest);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.MovieInterests.Remove(interest);
            if (await FindAsync(userId, movieId, CancellationToken.None) is { } concurrent)
                return concurrent;
            throw;
        }

        return await FindAsync(userId, movieId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Movie);
    }

    public async Task<RemoveMovieInterestResponse> RemoveAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    )
    {
        var deleted = await db
            .MovieInterests.Where(i => i.UserId == userId && i.MovieId == movieId)
            .ExecuteDeleteAsync(cancellationToken);
        return new RemoveMovieInterestResponse(movieId, deleted > 0);
    }

    public async Task<OffsetPageResult<AdminMovieInterestDto>> GetCountsAdminAsync(
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.Movies.AsNoTracking().AsQueryable();
        if (EnumConverters.TryParseEnum<MovieStatus>(status, out var parsedStatus))
            query = query.Where(m => m.Status == parsedStatus);
        query = query.ApplySearch(
            search,
            (q, pattern) =>
                q.Where(m =>
                    EF.Functions.ILike(EF.Functions.Unaccent(m.Title), EF.Functions.Unaccent(pattern))
                    || EF.Functions.ILike(
                        EF.Functions.Unaccent(m.OriginalTitle ?? ""),
                        EF.Functions.Unaccent(pattern)
                    )
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Select(m => new
            {
                m.MovieId,
                m.Title,
                m.PosterUrl,
                m.ReleaseDate,
                m.Status,
                Count = db.MovieInterests.Count(i => i.MovieId == m.MovieId),
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.ReleaseDate)
            .ThenBy(x => x.MovieId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new AdminMovieInterestDto(
                x.MovieId,
                x.Title,
                x.PosterUrl,
                x.ReleaseDate,
                x.Status,
                x.Count
            ))
            .ToList();
        return OffsetPageResult<AdminMovieInterestDto>.Create(items, page, pageSize, total);
    }

    public Task<List<int>> GetInterestedUserIdsAsync(
        int movieId,
        CancellationToken cancellationToken
    ) =>
        db.MovieInterests.AsNoTracking()
            .Where(i => i.MovieId == movieId)
            .OrderBy(i => i.UserId)
            .Select(i => i.UserId)
            .ToListAsync(cancellationToken);

    public Task<bool> IsInterestedAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    ) => db.MovieInterests.AnyAsync(i => i.UserId == userId && i.MovieId == movieId, cancellationToken);

    private Task<MovieInterestDto?> FindAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken
    ) =>
        ProjectDtos(
                db.MovieInterests.AsNoTracking()
                    .Where(i => i.UserId == userId && i.MovieId == movieId)
            )
            .FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<MovieInterestDto> ProjectDtos(IQueryable<MovieInterest> query) =>
        query.Select(i => new MovieInterestDto(
            i.MovieId,
            i.Movie.Title,
            i.Movie.PosterUrl,
            i.Movie.ReleaseDate,
            i.Movie.Status,
            i.CreatedAt
        ));
}
