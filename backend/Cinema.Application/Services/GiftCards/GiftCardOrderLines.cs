using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.GiftCards;

internal static class GiftCardOrderLines
{
    public static OrderLine Create(
        decimal amount,
        string recipientEmail,
        string recipientName,
        string? message,
        DateTime? sendAt
    ) =>
        new()
        {
            LineType = OrderLineType.GiftCard,
            Description = string.Format(
                Messages.Order_GiftCardLineDescription,
                GiftCardRules.FormatMoney(amount)
            ),
            Quantity = 1,
            UnitListPrice = amount,
            LockedUnitPrice = amount,
            GiftRecipientEmail = recipientEmail,
            GiftRecipientName = recipientName,
            GiftMessage = GiftCardRules.NormalizeText(message, FieldLengths.GiftCardMessage),
            GiftSendAt = sendAt,
        };
}
