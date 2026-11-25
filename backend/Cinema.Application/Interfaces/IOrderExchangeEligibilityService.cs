using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public record OrderExchangeCheckRequest(
    int OrderId,
    int NewShowtimeId,
    IReadOnlyCollection<int>? OrderLineIds,
    SalesChannel Channel
);

public record OrderExchangeEligibility(
    int OrderId,
    int ExchangeNumber,
    IReadOnlyList<int> OrderLineIds,
    int OldShowtimeId,
    int NewShowtimeId
);

public interface IOrderExchangeEligibilityService
{
    Task<OrderExchangeEligibility> EnsureEligibleAsync(
        OrderExchangeCheckRequest request,
        CancellationToken cancellationToken
    );
}
