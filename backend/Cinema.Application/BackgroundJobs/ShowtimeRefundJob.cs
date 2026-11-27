using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.BackgroundJobs;

public class ShowtimeRefundJob(
    IAppDbContext db,
    IPaymentRefundService paymentRefunds,
    IRefundManualHandoffService manualHandoff,
    ILogger<ShowtimeRefundJob> logger,
    IOptions<BackgroundJobOptions>? jobOptions = null
)
{
    private readonly int _retryAttempts = (jobOptions?.Value ?? new()).RetryAttempts;

    public async Task RunAsync(
        int orderId,
        PerformContext? context,
        CancellationToken cancellationToken
    )
    {
        var pendingIds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Pending
                && (t.Method == PaymentMethod.VNPay
                    || t.Method == PaymentMethod.Momo
                    || t.Method == PaymentMethod.Stripe)
            )
            .OrderBy(t => t.PaymentTransactionId)
            .Select(t => t.PaymentTransactionId)
            .ToListAsync(cancellationToken);
        if (pendingIds.Count == 0)
            return;

        var failures = new List<string>();
        foreach (var refundId in pendingIds)
        {
            var result = await paymentRefunds.ExecuteAsync(refundId, false, cancellationToken);
            if (result.Status == PaymentStatus.Pending)
                failures.Add(result.Message ?? Messages.Refund_ExecutionError);
        }

        if (failures.Count == 0)
        {
            logger.LogInformation(
                "ShowtimeRefunded OrderId {OrderId} Refunds {Count}",
                orderId,
                pendingIds.Count
            );
            return;
        }

        logger.LogWarning(
            "ShowtimeCancelRefundFailed OrderId {OrderId} Failed {Count} Message {Message}",
            orderId,
            failures.Count,
            failures[0]
        );

        var failureMessage = string.Format(Messages.Refund_JobFailed, orderId, failures[0]);
        var retryCount = context?.GetJobParameter<int>("RetryCount") ?? _retryAttempts;
        if (retryCount < _retryAttempts)
            throw new InvalidOperationException(failureMessage);

        await MarkManualAsync(orderId, cancellationToken);
        throw new InvalidOperationException(failureMessage);
    }

    private async Task MarkManualAsync(int orderId, CancellationToken cancellationToken)
    {
        var chargeIds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Pending
                && t.OriginalTransactionId != null
                && (t.Method == PaymentMethod.VNPay
                    || t.Method == PaymentMethod.Momo
                    || t.Method == PaymentMethod.Stripe)
            )
            .Select(t => t.OriginalTransactionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var marked = await manualHandoff.MarkChargesManualAsync(
            orderId,
            chargeIds,
            true,
            cancellationToken
        );
        if (marked > 0)
            logger.LogCritical(
                "ShowtimeRefundNeedsManual OrderId {OrderId} Charges {Count}",
                orderId,
                marked
            );
    }
}
