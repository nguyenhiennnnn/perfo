using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardRefundHandler
{
    Task<PaymentTransaction> ReserveAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    );
}
