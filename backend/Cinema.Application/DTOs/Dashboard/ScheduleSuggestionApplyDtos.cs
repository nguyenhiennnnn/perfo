using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.DTOs.Dashboard;

public record ApplyScheduleSuggestionsRequest(List<string> SuggestionIds);

public record ScheduleSuggestionNotApplicableDto(string Id, string Reason);

public record ApplyScheduleSuggestionsResponseDto(
    AutoScheduleRequest? Request,
    AutoScheduleResponseDto? Preview,
    List<string> AppliedIds,
    List<ScheduleSuggestionNotApplicableDto> NotApplicable
);
