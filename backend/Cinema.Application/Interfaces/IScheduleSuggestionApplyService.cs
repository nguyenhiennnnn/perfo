using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces;

public interface IScheduleSuggestionApplyService
{
    Task<ApplyScheduleSuggestionsResponseDto> PreviewAsync(
        ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken
    );
}
