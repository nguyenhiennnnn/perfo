using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardEmailService(IAppDbContext db, INotificationService notificationService)
    : IGiftCardEmailService
{
    public async Task SendToRecipientAsync(int giftCardId, CancellationToken cancellationToken)
    {
        var (card, _) = await LoadAsync(giftCardId, cancellationToken);
        if (card is not null)
            notificationService.SendGiftCardReceived(card.RecipientEmail, card);
    }

    public async Task SendPurchaserConfirmationAsync(
        int giftCardId,
        CancellationToken cancellationToken
    )
    {
        var (card, purchaserEmail) = await LoadAsync(giftCardId, cancellationToken);
        if (card is not null && purchaserEmail is not null)
            notificationService.SendGiftCardPurchaserConfirmation(purchaserEmail, card);
    }

    private async Task<(GiftCardEmailData? Data, string? PurchaserEmail)> LoadAsync(
        int giftCardId,
        CancellationToken cancellationToken
    )
    {
        var row = await db
            .GiftCards.AsNoTracking()
            .Where(c =>
                c.GiftCardId == giftCardId
                && c.RecipientEmail != null
                && c.Status != GiftCardStatus.Cancelled
                && c.Status != GiftCardStatus.Expired
            )
            .Select(c => new
            {
                c.RecipientEmail,
                c.RecipientName,
                c.Message,
                c.Code,
                c.FaceValue,
                c.ExpiresAt,
                PurchaserName = c.PurchaserUser.FullName,
                PurchaserEmail = c.PurchaserUser.Email,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return (null, null);

        return (
            new GiftCardEmailData(
                row.RecipientName ?? row.RecipientEmail!,
                row.RecipientEmail!,
                row.PurchaserName,
                row.Message,
                row.Code,
                row.FaceValue,
                row.ExpiresAt
            ),
            row.PurchaserEmail
        );
    }
}
