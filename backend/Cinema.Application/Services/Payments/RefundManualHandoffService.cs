using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Interfaces.Support;
using Cinema.Application.Interfaces.Users;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Payments;

public class RefundManualHandoffService(
    IAppDbContext db,
    ISupportAutoRequestService supportAutoRequests,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    ILogger<RefundManualHandoffService> logger
) : IRefundManualHandoffService
{
    public async Task<int> MarkChargesManualAsync(
        int orderId,
        IReadOnlyCollection<int> chargeTransactionIds,
        bool showtimeCancelled,
        CancellationToken cancellationToken
    )
    {
        if (chargeTransactionIds.Count == 0)
            return 0;

        var chargeIds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                chargeTransactionIds.Contains(t.PaymentTransactionId)
                && t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && !t.NeedsManualRefund
            )
            .Select(t => t.PaymentTransactionId)
            .ToListAsync(cancellationToken);
        if (chargeIds.Count == 0)
            return 0;

        var marked = await db
            .PaymentTransactions.Where(t =>
                chargeIds.Contains(t.PaymentTransactionId) && !t.NeedsManualRefund
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.NeedsManualRefund, true),
                cancellationToken
            );
        if (marked == 0)
            return 0;

        logger.LogCritical(
            "PendingRefundNeedsManual OrderId {OrderId} ChargeTransactionIds {ChargeIds} — cần hoàn tiền thủ công.",
            orderId,
            string.Join(",", chargeIds)
        );

        foreach (var chargeId in chargeIds)
        {
            try
            {
                await supportAutoRequests.EnsureForTransactionAsync(
                    chargeId,
                    SupportAutoRequestReason.GatewayRefundUnresolved,
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "SupportAutoRequestFailed PaymentTransactionId {Id} OrderId {OrderId}",
                    chargeId,
                    orderId
                );
            }
        }

        await NotifyCustomerAsync(orderId, chargeIds, showtimeCancelled, cancellationToken);
        return marked;
    }

    private async Task NotifyCustomerAsync(
        int orderId,
        IReadOnlyCollection<int> chargeIds,
        bool showtimeCancelled,
        CancellationToken cancellationToken
    )
    {
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

        var amountDue =
            await db
                .PaymentTransactions.AsNoTracking()
                .Where(t =>
                    t.Type == PaymentTransactionType.Refund
                    && t.Status == PaymentStatus.Pending
                    && t.OriginalTransactionId != null
                    && chargeIds.Contains(t.OriginalTransactionId.Value)
                )
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var customerName = order.CustomerName ?? order.FullName;
        if (showtimeCancelled)
            notificationService.SendShowtimeCancelledManualRefund(
                order.Email,
                customerName,
                order.OrderCode,
                amountDue
            );
        else
            notificationService.SendRefundManualNotice(
                order.Email,
                customerName,
                order.OrderCode,
                amountDue
            );
    }
}
