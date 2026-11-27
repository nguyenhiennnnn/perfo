using Cinema.Application.Caching;
using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class ScheduleSuggestionApplyService(
    IAppDbContext db,
    IAppCache cache,
    IScheduleSuggestionService suggestionService,
    IShowtimeAutoScheduleService autoScheduleService
) : IScheduleSuggestionApplyService
{
    private const int MaxSelected = 20;

    public async Task<ApplyScheduleSuggestionsResponseDto> PreviewAsync(
        ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken
    )
    {
        var ids = (request.SuggestionIds ?? []).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
        if (ids.Count == 0)
            throw new ValidationAppException(Messages.Invalid_SuggestionIdsRequired);
        if (ids.Count > MaxSelected)
            throw new ValidationAppException(
                string.Format(Messages.Invalid_SuggestionIdsTooMany, MaxSelected)
            );

        var current = await suggestionService.GetAsync(cancellationToken);
        var byId = current.Items.ToDictionary(x => x.Id);
        var missing = ids.Where(i => !byId.ContainsKey(i)).ToList();
        if (missing.Count > 0)
            throw new ValidationAppException(
                string.Format(Messages.Invalid_SuggestionIdsNotFound, string.Join(", ", missing))
            );

        var selected = ids.Select(i => byId[i]).ToList();
        var applicable = new List<ScheduleSuggestionDto>();
        var notApplicable = new List<ScheduleSuggestionNotApplicableDto>();
        foreach (var s in selected)
        {
            if (s.Action is ScheduleSuggestionAction.ReduceShowtimes
                or ScheduleSuggestionAction.MoveToSmallerRoom
                or ScheduleSuggestionAction.MoveToBusierSlot)
                notApplicable.Add(new(s.Id, Messages.Schedule_Apply_NotApplicable_Reduce));
            else if (s.RoomId is null)
                notApplicable.Add(new(s.Id, Messages.Schedule_Apply_NotApplicable_NoRoom));
            else
                applicable.Add(s);
        }

        if (applicable.Count == 0)
            return new ApplyScheduleSuggestionsResponseDto(null, null, [], notApplicable);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var date = BusinessDay
            .Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime)
            .AddDays(1);

        var movieIds = applicable.Select(s => s.MovieId).Distinct().ToList();
        var latestPrices = await db
            .Showtimes.AsNoTracking()
            .WhereRegularKind()
            .Where(s => movieIds.Contains(s.MovieId))
            .GroupBy(s => s.MovieId)
            .Select(g => new
            {
                MovieId = g.Key,
                Price = g.OrderByDescending(s => s.StartTime).Select(s => s.BasePrice).First(),
            })
            .ToDictionaryAsync(x => x.MovieId, x => x.Price, cancellationToken);

        var movies = applicable
            .GroupBy(s => s.MovieId)
            .Select(g => new AutoScheduleMovieRequest(
                g.Key,
                g.Count(),
                latestPrices.GetValueOrDefault(g.Key)
            ))
            .ToList();
        var roomIds = applicable.Select(s => s.RoomId!.Value).Distinct().ToList();

        var autoRequest = new AutoScheduleRequest(
            date,
            movies,
            settings.MinTicketPrice,
            roomIds
        );
        var preview = await autoScheduleService.PreviewAutoScheduleAsync(
            autoRequest,
            cancellationToken
        );

        return new ApplyScheduleSuggestionsResponseDto(
            autoRequest,
            preview,
            applicable.Select(s => s.Id).ToList(),
            notApplicable
        );
    }
}
