using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.GiftCards;

public record IssueGiftCardRequest(
    GiftCardType Type,
    decimal FaceValue,
    int PurchaserUserId,
    string? RecipientEmail = null,
    string? RecipientName = null,
    string? Message = null,
    DateTime? SendAt = null,
    int? OrderId = null,
    DateTime? ExpiresAt = null
);

public record GiftCardBalanceCheckDto(
    int GiftCardId,
    decimal Balance,
    decimal LedgerBalance,
    decimal ReservedAmount,
    decimal LedgerReserved
)
{
    public bool IsConsistent => Balance == LedgerBalance && ReservedAmount == LedgerReserved;
}

public record PurchaseGiftCardRequest(
    decimal Amount,
    string RecipientEmail,
    string RecipientName,
    string? Message,
    DateTime? SendAt
);
