namespace Cinema.Application.DTOs.GiftCards;

public record AdminGiftCardListFilter(string? Search, string? Status, string? Type);

public record AdminGiftCardListItemDto(
    int GiftCardId,
    string MaskedCode,
    string Type,
    string Status,
    decimal FaceValue,
    decimal Balance,
    decimal ReservedAmount,
    int PurchaserUserId,
    string PurchaserName,
    string PurchaserEmail,
    string? RecipientName,
    string? RecipientEmail,
    DateTime? SendAt,
    DateTime ExpiresAt,
    DateTime CreatedAt
);

public record AdminGiftCardLedgerEntryDto(
    long GiftCardLedgerId,
    string EntryType,
    decimal BalanceChange,
    decimal ReservedChange,
    decimal BalanceAfter,
    decimal ReservedAfter,
    int? OrderId,
    DateTime OccurredAt
);
