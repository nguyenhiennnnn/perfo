using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.GiftCards;

public record CancelGiftCardRequest(string Reason, GiftCardCancelBalanceAction? BalanceAction);

public record CancelGiftCardResponse(
    int GiftCardId,
    decimal CancelledBalance,
    GiftCardCancelBalanceAction? BalanceAction,
    int? CreditGiftCardId
);

public record GiftCardLookupDto(
    string Code,
    string Type,
    string Status,
    decimal Balance,
    decimal AvailableBalance,
    DateTime ExpiresAt
);
