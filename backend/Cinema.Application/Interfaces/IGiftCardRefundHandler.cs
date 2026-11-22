using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces;

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
