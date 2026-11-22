using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
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
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
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
        var marked = await db
            .PaymentTransactions.Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Pending
                && !t.NeedsManualRefund
                && (t.Method == PaymentMethod.VNPay
                    || t.Method == PaymentMethod.Momo
                    || t.Method == PaymentMethod.Stripe)
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.NeedsManualRefund, true),
                cancellationToken
            );
        if (marked == 0)
            return;

        logger.LogCritical(
            "ShowtimeRefundNeedsManual OrderId {OrderId} Refunds {Count}",
            orderId,
            marked
        );

        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId && o.UserId != null)
            .Select(o => new
            {
                o.OrderCode,
                o.CustomerName,
                UserId = o.UserId!.Value,
                o.User!.Email,
                o.User.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.Email))
            return;

        if (!await preferences.CanSendAsync(order.UserId, EmailCategory.Service, cancellationToken))
            return;

        var amountDue = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Pending
                && t.NeedsManualRefund
                && t.Method != PaymentMethod.Cash
            )
            .SumAsync(t => (decimal?)t.Amount, cancellationToken);

        notificationService.SendShowtimeCancelledManualRefund(
            order.Email,
            order.CustomerName ?? order.FullName,
            order.OrderCode,
            amountDue ?? 0m
        );
    }
}
