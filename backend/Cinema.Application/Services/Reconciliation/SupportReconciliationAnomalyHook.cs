using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Application.Interfaces.Support;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Reconciliation;

public sealed class SupportReconciliationAnomalyHook(
    ISupportAutoRequestService supportAutoRequests,
    ILogger<SupportReconciliationAnomalyHook> logger
) : IReconciliationAnomalyHook
{
    public async Task OnAnomalyAsync(ReconciliationItem item, CancellationToken cancellationToken)
    {
        if (
            item.Result != ReconciliationResult.PaidAtGatewayOrderUnconfirmed
            || item.PaymentTransactionId is not { } transactionId
        )
            return;

        try
        {
            await supportAutoRequests.EnsureForTransactionAsync(
                transactionId,
                SupportAutoRequestReason.ChargedOrderNotConfirmed,
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "SupportAutoRequestFailed ReconciliationItemId {ItemId} PaymentTransactionId {TransactionId}",
                item.ReconciliationItemId,
                transactionId
            );
        }
    }
}
