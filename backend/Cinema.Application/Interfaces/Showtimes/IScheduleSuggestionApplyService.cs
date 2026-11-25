using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IScheduleSuggestionApplyService
{
    Task<ApplyScheduleSuggestionsResponseDto> PreviewAsync(
        ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken
    );
}
