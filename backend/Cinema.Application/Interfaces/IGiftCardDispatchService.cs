namespace Cinema.Application.Interfaces;

public interface IGiftCardDispatchService
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken);

    Task ResendAsync(int giftCardId, CancellationToken cancellationToken);
}

public interface IGiftCardEmailService
{
    Task SendToRecipientAsync(int giftCardId, CancellationToken cancellationToken);

    Task SendPurchaserConfirmationAsync(int giftCardId, CancellationToken cancellationToken);
}
