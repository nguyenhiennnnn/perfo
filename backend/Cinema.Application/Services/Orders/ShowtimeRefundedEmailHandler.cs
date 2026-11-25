using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Users;


namespace Cinema.Application.Services.Orders;

public class ShowtimeRefundedEmailHandler(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService
) : IBackgroundEventHandler<OrderRefunded>
{
    public async Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken)
    {
        var refund = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.PaymentTransactionId == businessEvent.RefundTransactionId
                && t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Success
            )
            .Select(t => new { t.Method })
            .FirstOrDefaultAsync(cancellationToken);
        if (refund is null || !refund.Method.IsGatewayMethod())
            return;

        var order = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.OrderId == businessEvent.OrderId
                && o.UserId != null
                && o.Status == OrderStatus.Cancelled
            )
            .Where(ShowtimeCancellationRouting.CancelledByShowtime)
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

        var gatewayRefunds = db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == businessEvent.OrderId
                && t.Type == PaymentTransactionType.Refund
                && (
                    t.Method == PaymentMethod.VNPay
                    || t.Method == PaymentMethod.Momo
                    || t.Method == PaymentMethod.Stripe
                )
            );

        if (await gatewayRefunds.AnyAsync(t => t.Status == PaymentStatus.Pending, cancellationToken))
            return;

        var lastRefundId = await gatewayRefunds
            .Where(t => t.Status == PaymentStatus.Success)
            .MaxAsync(t => t.PaymentTransactionId, cancellationToken);
        if (lastRefundId != businessEvent.RefundTransactionId)
            return;

        if (!await preferences.CanSendAsync(order.UserId, EmailCategory.Service, cancellationToken))
            return;

        var total = await gatewayRefunds
            .Where(t => t.Status == PaymentStatus.Success)
            .SumAsync(t => t.Amount, cancellationToken);

        notificationService.SendShowtimeCancelledRefunded(
            order.Email,
            order.CustomerName ?? order.FullName,
            order.OrderCode,
            total
        );
    }
}
