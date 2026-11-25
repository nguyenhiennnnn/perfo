using Cinema.Application.Events;
using Cinema.Application.Interfaces;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardRecipientEmailHandler(IGiftCardEmailService emailService)
    : IBackgroundEventHandler<GiftCardSent>
{
    public Task HandleAsync(GiftCardSent businessEvent, CancellationToken cancellationToken) =>
        emailService.SendToRecipientAsync(businessEvent.GiftCardId, cancellationToken);
}

public class GiftCardPurchaserEmailHandler(IGiftCardEmailService emailService)
    : IBackgroundEventHandler<GiftCardSent>
{
    public Task HandleAsync(GiftCardSent businessEvent, CancellationToken cancellationToken) =>
        emailService.SendPurchaserConfirmationAsync(businessEvent.GiftCardId, cancellationToken);
}
