using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardRevenueService
{
    Task RecognizeAsync(
        PaymentTransaction charge,
        int giftCardId,
        decimal amount,
        DateTime occurredAt,
        CancellationToken cancellationToken
    );

    Task ReverseAsync(
        PaymentTransaction refund,
        PaymentTransaction charge,
        decimal amount,
        DateTime occurredAt,
        CancellationToken cancellationToken
    );
}
