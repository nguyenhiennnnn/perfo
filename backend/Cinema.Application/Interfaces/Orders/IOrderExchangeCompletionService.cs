using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public enum OrderExchangeCompletionStatus
{
    Completed,
    AlreadyCompleted,
    AwaitingPayment,
    NotCompletable,
}

public sealed record OrderExchangeCompletion(
    OrderExchangeCompletionStatus Status,
    OrderExchangeSettlementResult? Settlement
);

public interface IOrderExchangeCompletionService
{
    Task<OrderExchangeCompletion> TryCompleteAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    );
}
