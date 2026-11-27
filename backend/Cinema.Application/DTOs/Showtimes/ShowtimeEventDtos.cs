namespace Cinema.Application.DTOs.Showtimes;

public record UpsertShowtimeEventRequest(string AdjustmentMode, decimal AdjustmentValue);

public record ShowtimeEventDto(int ShowtimeId, string AdjustmentMode, decimal AdjustmentValue);
