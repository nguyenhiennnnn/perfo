namespace Cinema.Application.DTOs.Dashboard;

public enum ScheduleSuggestionType
{
    HighDemand,
    LowDemand,
    EmptySlot,
}

public enum ScheduleSuggestionAction
{
    AddShowtimes,
    ReduceShowtimes,
    MoveToSmallerRoom,
    MoveToBusierSlot,
    FillEmptySlot,
}

public record ScheduleSuggestionDto(
    string Id,
    ScheduleSuggestionType Type,
    ScheduleSuggestionAction Action,
    int MovieId,
    string? MovieTitle,
    int? RoomId,
    string? RoomName,
    string? TimeSlot,
    int? FromRoomId,
    string? FromRoomName,
    int ShowtimeCount,
    decimal OccupancyPercent,
    int? TargetShowtimeCount,
    decimal? TargetOccupancyPercent,
    string Reason
);

public record ScheduleSuggestionsDto(
    DateOnly From,
    DateOnly To,
    int HighOccupancyPercent,
    int LowOccupancyPercent,
    int MinShowtimes,
    IReadOnlyList<ScheduleSuggestionDto> Items
);
