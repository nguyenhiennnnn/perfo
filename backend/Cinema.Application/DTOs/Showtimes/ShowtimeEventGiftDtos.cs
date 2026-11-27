namespace Cinema.Application.DTOs.Showtimes;

public record ShowtimeEventGiftRequest(int InventoryItemId, int Quantity, int QuantityPerTicket = 1);

public record UpsertShowtimeEventGiftsRequest(List<ShowtimeEventGiftRequest>? Gifts);

public record ShowtimeEventGiftDto(
    int InventoryItemId,
    string Code,
    string Name,
    string Unit,
    int Quantity,
    int QuantityPerTicket,
    int Held,
    int Given
);

public record ShowtimeEventGiftsDto(int ShowtimeId, IReadOnlyList<ShowtimeEventGiftDto> Gifts);
