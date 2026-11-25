using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IScheduleSuggestionService
{
    Task<ScheduleSuggestionsDto> GetAsync(CancellationToken cancellationToken);
}
