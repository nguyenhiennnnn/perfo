namespace Cinema.Application.Interfaces.Ledger;

public record OrderPaymentSummary(
    int OrderId,
    decimal FinalAmount,
    decimal CollectedAmount,
    decimal RefundedAmount,
    decimal PendingRefundAmount,
    decimal NetPaidAmount,
    decimal OutstandingAmount,
    decimal OverpaidAmount,
    bool IsFullyPaid
);

public interface IPaymentLedgerService
{
    Task<OrderPaymentSummary> GetSummaryAsync(int orderId, CancellationToken cancellationToken);

    Task<decimal> GetNetPaidAmountAsync(int orderId, CancellationToken cancellationToken);

    Task<decimal> GetRefundedAmountAsync(int orderId, CancellationToken cancellationToken);

    Task<bool> IsFullyPaidAsync(int orderId, CancellationToken cancellationToken);

    Task<decimal> GetRefundableAmountAsync(
        int chargeTransactionId,
        CancellationToken cancellationToken
    );
}
