namespace Cinema.Application.DTOs.GiftCards;

public sealed record GiftCardBulkImportRow(
    string? RecipientName,
    string? RecipientEmail,
    string? Amount,
    string? Message,
    decimal? FaceValue = null
);
