namespace Cinema.Application.DTOs.Showtimes;

public record EventGiftHandoverRequest(string QrCode);

public record EventGiftHandoverLineDto(
    int ReservationId,
    int InventoryItemId,
    string Name,
    string Unit,
    int Quantity,
    int Given,
    int Remaining
);

public record EventGiftHandoverHistoryDto(
    long HandoverId,
    string GiftName,
    int Quantity,
    DateTime HandedAt,
    int HandedByUserId,
    string HandedByName
);

public record EventGiftHandoverTicketDto(
    int OrderId,
    string OrderCode,
    int OrderLineId,
    int ShowtimeId,
    string OrderStatus,
    string? CustomerName,
    int TotalRemaining,
    IReadOnlyList<EventGiftHandoverLineDto> Gifts,
    IReadOnlyList<EventGiftHandoverHistoryDto> History
);

public record EventGiftHandoverResponse(
    EventGiftHandoverTicketDto Ticket,
    int HandedQuantity,
    DateTime HandedAt
);
