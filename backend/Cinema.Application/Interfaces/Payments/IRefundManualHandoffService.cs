namespace Cinema.Application.Interfaces.Payments;

public interface IRefundManualHandoffService
{
    Task<int> MarkChargesManualAsync(
        int orderId,
        IReadOnlyCollection<int> chargeTransactionIds,
        bool showtimeCancelled,
        CancellationToken cancellationToken
    );
}
