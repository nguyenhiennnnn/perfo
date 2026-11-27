namespace Cinema.Application.Interfaces.Support;

public enum SupportAutoRequestReason
{
    ChargedOrderNotConfirmed,
    OverpaymentRefundFailed,
    GatewayRefundUnresolved,
}

public interface ISupportAutoRequestService
{
    Task<int?> EnsureForTransactionAsync(
        int paymentTransactionId,
        SupportAutoRequestReason reason,
        CancellationToken cancellationToken
    );
}
