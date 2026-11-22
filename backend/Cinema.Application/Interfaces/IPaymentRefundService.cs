using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public record PaymentRefundResult(
    int RefundTransactionId,
    PaymentStatus Status,
    decimal Amount,
    string? Message
)
{
    public bool Success => Status == PaymentStatus.Success;
}

public interface IPaymentRefundService
{
    Task<PaymentRefundResult> RefundAsync(
        int chargeTransactionId,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    );

    Task<PaymentTransaction> ReserveAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    );

    Task<PaymentTransaction> ReserveManualCashAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        CancellationToken cancellationToken
    );

    Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    );

    Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        bool failOnError,
        CancellationToken cancellationToken
    );

    Task<PaymentRefundResult> ConfirmCashRefundAsync(
        int refundTransactionId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    );
}
