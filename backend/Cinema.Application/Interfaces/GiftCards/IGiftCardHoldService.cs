namespace Cinema.Application.Interfaces.GiftCards;

public sealed record GiftCardHoldEntry(int GiftCardId, decimal Amount);

public interface IGiftCardHoldService
{
    Task<IReadOnlyList<GiftCardHoldEntry>> ListAsync(int orderId, CancellationToken cancellationToken);

    Task<decimal> GetReservedAmountAsync(int orderId, CancellationToken cancellationToken);

    Task ReserveAsync(
        int orderId,
        int giftCardId,
        decimal amount,
        CancellationToken cancellationToken
    );

    Task ReleaseAsync(int orderId, int giftCardId, CancellationToken cancellationToken);

    Task TrimAsync(int orderId, decimal limit, CancellationToken cancellationToken);
}
