using Cinema.Application.Interfaces.Payments;

namespace Cinema.Application.Interfaces.Orders;

public record OrderLineRefundItem(int OrderLineId, int Quantity);

public record OrderLineRefundCommand(
    int OrderId,
    IReadOnlyList<OrderLineRefundItem> Lines,
    int? PerformedByUserId,
    string? Note,
    decimal Percent = 100m
);

public record OrderLineRefundLineResult(int OrderLineId, int Quantity, decimal Amount);

public record OrderLineRefundResult(
    decimal TotalAmount,
    IReadOnlyList<OrderLineRefundLineResult> Lines,
    IReadOnlyList<PaymentRefundResult> Transactions
)
{
    public bool Success => Transactions.All(t => t.Success);
}

public interface IOrderLineRefundService
{
    Task<OrderLineRefundResult> RefundLinesAsync(
        OrderLineRefundCommand command,
        CancellationToken cancellationToken
    );
}
