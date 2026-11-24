using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces;

public interface IScheduleSuggestionService
{
    Task<ScheduleSuggestionsDto> GetAsync(CancellationToken cancellationToken);
}
