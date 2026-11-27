using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.BackgroundJobs;

public class PendingGatewayRefundJob(
    IAppDbContext db,
    IPaymentRefundService paymentRefunds,
    IRefundManualHandoffService manualHandoff,
    ILogger<PendingGatewayRefundJob> logger,
    IOptions<BackgroundJobOptions>? jobOptions = null
)
{
    private const int MaxScan = 500;

    private readonly BackgroundJobOptions _options = jobOptions?.Value ?? new();

    private int MaxRetries => _options.RetryAttempts;

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var minDelaySeconds = _options.RetryDelaysSeconds.Length == 0
            ? 0
            : _options.RetryDelaysSeconds.Min();
        var firstEligibleBefore = now.AddSeconds(-minDelaySeconds);

        var candidates = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Pending
                && (t.Method == PaymentMethod.VNPay
                    || t.Method == PaymentMethod.Momo
                    || t.Method == PaymentMethod.Stripe)
                && !t.OriginalTransaction!.NeedsManualRefund
                && (t.LastRefundAttemptAt ?? t.CreatedAt) <= firstEligibleBefore
            )
            .OrderBy(t => t.LastRefundAttemptAt ?? t.CreatedAt)
            .ThenBy(t => t.PaymentTransactionId)
            .Take(MaxScan)
            .Select(t => new Candidate(
                t.PaymentTransactionId,
                t.OrderId,
                t.OriginalTransactionId!.Value,
                t.RefundAttemptCount,
                t.LastRefundAttemptAt ?? t.CreatedAt
            ))
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0)
            return;

        var exhausted = new List<Candidate>();
        var retried = 0;
        var completed = 0;
        foreach (var candidate in candidates)
        {
            var eligibleBefore = now.AddSeconds(-DelaySeconds(candidate.Attempts));
            if (candidate.LastActivityAt > eligibleBefore)
                continue;

            if (candidate.Attempts > MaxRetries)
            {
                exhausted.Add(candidate);
                continue;
            }

            try
            {
                var result = await paymentRefunds.RetryPendingAsync(
                    candidate.RefundTransactionId,
                    eligibleBefore,
                    cancellationToken
                );
                retried++;
                if (result.Success)
                    completed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "PendingRefundRetryError RefundTransactionId {RefundId} OrderId {OrderId}",
                    candidate.RefundTransactionId,
                    candidate.OrderId
                );
            }
        }

        foreach (var group in exhausted.GroupBy(c => c.OrderId))
        {
            try
            {
                await manualHandoff.MarkChargesManualAsync(
                    group.Key,
                    [.. group.Select(c => c.ChargeTransactionId).Distinct()],
                    false,
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "PendingRefundManualHandoffError OrderId {OrderId}",
                    group.Key
                );
            }
        }

        logger.LogInformation(
            "PendingRefundSweep Retried {Retried} Completed {Completed} Exhausted {Exhausted}",
            retried,
            completed,
            exhausted.Count
        );
    }

    private int DelaySeconds(int attempts)
    {
        var delays = _options.RetryDelaysSeconds;
        return delays.Length == 0 ? 0 : delays[Math.Clamp(attempts - 1, 0, delays.Length - 1)];
    }

    private sealed record Candidate(
        int RefundTransactionId,
        int OrderId,
        int ChargeTransactionId,
        int Attempts,
        DateTime LastActivityAt
    );
}
