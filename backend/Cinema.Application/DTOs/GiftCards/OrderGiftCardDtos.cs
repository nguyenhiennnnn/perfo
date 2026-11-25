namespace Cinema.Application.DTOs.GiftCards;

public record ApplyGiftCardRequest(string Code);

public record OrderGiftCardItemDto(int GiftCardId, string Code, string Type, decimal Amount);

public record OrderGiftCardResponse(
    int OrderId,
    string OrderCode,
    string Status,
    decimal FinalAmount,
    decimal GiftCardAmount,
    decimal PayableAmount,
    bool IsFullyCovered,
    IReadOnlyList<OrderGiftCardItemDto> Cards
);
