using Cinema.Application.Events;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderReleaseService(
    IAppDbContext db,
    IOrderResourceHoldService resourceHolds,
    IOrderRefundService orderRefunds,
    IPaymentRefundService paymentRefunds,
    IBusinessEventDispatcher eventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    IAbuseSignalService abuseSignals,
    ICustomerRestrictionService restrictions,
    ILogger<OrderReleaseService> logger
) : IOrderReleaseService
{
    public Task<bool> ExpireAsync(int orderId, CancellationToken cancellationToken) =>
        ReleasePendingAsync(orderId, OrderStatus.Expired, null, null, true, cancellationToken);

    public Task<bool> CancelPendingAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    ) =>
        ReleasePendingAsync(
            orderId,
            OrderStatus.Cancelled,
            reason,
            cancelledBy,
            true,
            cancellationToken
        );

    public Task<bool> CancelPendingWithoutRefundAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    ) =>
        ReleasePendingAsync(
            orderId,
            OrderStatus.Cancelled,
            reason,
            cancelledBy,
            false,
            cancellationToken
        );

    private async Task<bool> ReleasePendingAsync(
        int orderId,
        OrderStatus target,
        CancelReason? reason,
        CancelledBy? cancelledBy,
        bool refundCollected,
        CancellationToken cancellationToken
    )
    {
        var releasedSeats = new List<(int ShowtimeId, int SeatId)>();
        var released = false;
        IReadOnlyList<OrderRefundReservation> reservations = [];
        // Cổng ngoài chỉ được gọi sau khi giao dịch của chính hàm này đã commit. Nếu caller đã mở
        // giao dịch (hủy suất chiếu, hủy thuê phòng...), khoản hoàn ở lại Pending cho
        // PendingGatewayRefundJob quét.
        var ownsTransaction = db.Database.CurrentTransaction is null;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var now = DateTime.UtcNow;
                DateTime? cancelledAt = target == OrderStatus.Cancelled ? now : null;
                var claimed = await db
                    .Orders.Where(o => o.OrderId == orderId && o.Status == OrderStatus.Pending)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(o => o.Status, target)
                                .SetProperty(o => o.CancelReason, reason)
                                .SetProperty(o => o.CancelledBy, cancelledBy)
                                .SetProperty(o => o.CancelledAt, cancelledAt),
                        cancellationToken
                    );
                if (claimed == 0)
                    return;

                if (target == OrderStatus.Expired || cancelledBy == CancelledBy.Customer)
                {
                    var abandonedBy = await db
                        .Orders.AsNoTracking()
                        .Where(o =>
                            o.OrderId == orderId
                            && o.Channel == SalesChannel.Online
                            && o.Type == OrderType.Ticket
                            && o.UserId != null
                            && !o.PaymentTransactions.Any(t =>
                                t.Type == PaymentTransactionType.Charge
                                && t.Status == PaymentStatus.Success
                            )
                        )
                        .Select(o => o.UserId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (abandonedBy is { } abandonedUserId)
                    {
                        await abuseSignals.RecordHoldAbandonedAsync(
                            abandonedUserId,
                            orderId,
                            cancellationToken
                        );
                        if (target == OrderStatus.Expired)
                            await restrictions.SyncAsync(abandonedUserId, cancellationToken);
                    }
                }

                var seats = await db
                    .ShowtimeSeats.HeldFor(orderId)
                    .Select(ss => new { ss.ShowtimeId, ss.ShowtimeSeatId })
                    .ToListAsync(cancellationToken);
                if (seats.Count > 0)
                {
                    var seatIds = seats.Select(s => s.ShowtimeSeatId).ToList();
                    await db
                        .ShowtimeSeats.Where(ss =>
                            seatIds.Contains(ss.ShowtimeSeatId) && ss.HeldByOrderId == orderId
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                                    .SetProperty(x => x.HeldByUserId, (int?)null)
                                    .SetProperty(x => x.HeldByOrderId, (int?)null)
                                    .SetProperty(x => x.HeldUntil, (DateTime?)null),
                            cancellationToken
                        );
                    releasedSeats.AddRange(seats.Select(s => (s.ShowtimeId, s.ShowtimeSeatId)));
                    await SeatHoldEventRecorder.StageAsync(
                        db,
                        seatIds,
                        target == OrderStatus.Expired
                            ? SeatHoldEventType.Expired
                            : SeatHoldEventType.Released,
                        now,
                        cancellationToken
                    );
                    await db.SaveChangesAsync(cancellationToken);
                    foreach (var showtimeId in seats.Select(s => s.ShowtimeId).Distinct())
                        await eventDispatcher.PublishAsync(
                            new ShowtimeSeatsReleased(showtimeId, now),
                            cancellationToken
                        );
                }

                await db
                    .PaymentTransactions.Where(t =>
                        t.OrderId == orderId
                        && t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Pending
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                        cancellationToken
                    );

                await resourceHolds.ReleaseAllAsync(orderId, cancellationToken);

                if (refundCollected)
                    reservations = await ReserveCollectedRefundsAsync(
                        orderId,
                        target,
                        cancellationToken
                    );

                if (target == OrderStatus.Cancelled)
                    await eventDispatcher.PublishAsync(
                        new OrderCancelled(orderId, reason, cancelledBy, now),
                        cancellationToken
                    );

                released = true;
            },
            cancellationToken
        );

        if (!released)
            return false;

        foreach (var group in releasedSeats.GroupBy(s => s.ShowtimeId))
        {
            var seatIds = group.Select(s => s.SeatId).ToList();
            if (target == OrderStatus.Expired)
                await realtimeNotifier.NotifyHoldExpiredAsync(
                    group.Key,
                    orderId,
                    seatIds,
                    cancellationToken
                );
            else
                await realtimeNotifier.NotifySeatReleasedAsync(group.Key, seatIds, cancellationToken);
        }

        if (reservations.Count > 0)
        {
            if (ownsTransaction)
                await ExecuteGatewayRefundsAsync(orderId, reservations);
            else
                logger.LogInformation(
                    "OrderReleaseRefundDeferred OrderId {OrderId} Count {Count} — giao dịch của caller chưa commit, job quét hoàn tiền sẽ xử lý.",
                    orderId,
                    reservations.Count
                );
        }

        logger.LogInformation(
            "OrderReleased OrderId {OrderId} Status {Status} SeatsReleased {Count} RefundsReserved {Refunds}",
            orderId,
            target,
            releasedSeats.Count,
            reservations.Count
        );
        return true;
    }

    // Phải gọi trong giao dịch đang giữ khóa đơn. Phần còn hoàn được của từng giao dịch thu đã tính
    // cả hoàn Pending (GetRefundableAmountAsync) và trừ phí cổng (NetRefundable) nên chạy lặp không
    // hoàn đôi. Tiền mặt không có cổng để gọi: đặt chỗ hoàn thủ công, nhân viên xác nhận khi trả tiền.
    private async Task<IReadOnlyList<OrderRefundReservation>> ReserveCollectedRefundsAsync(
        int orderId,
        OrderStatus target,
        CancellationToken cancellationToken
    )
    {
        var plan = await orderRefunds.BuildPlanAsync(orderId, RefundScope.Full(false), cancellationToken);
        if (plan.TotalAmount <= 0m || plan.Items.Count == 0)
            return [];

        var orderCode = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => o.OrderCode)
            .FirstAsync(cancellationToken);
        var note = string.Format(
            target == OrderStatus.Expired
                ? Messages.Refund_OrderExpiredDescription
                : Messages.Refund_CancelOrderDescription,
            orderCode
        );

        var reservations = new List<OrderRefundReservation>();
        var otherItems = plan.Items.Where(i => i.Method != PaymentMethod.Cash).ToList();
        if (otherItems.Count > 0)
            reservations.AddRange(
                await orderRefunds.ReserveAsync(
                    new OrderRefundPlan(
                        orderId,
                        otherItems.Sum(i => i.Amount),
                        plan.FulfilledComboExcluded,
                        otherItems
                    ),
                    null,
                    note,
                    cancellationToken
                )
            );

        var cashItems = plan.Items.Where(i => i.Method == PaymentMethod.Cash).ToList();
        if (cashItems.Count > 0)
        {
            var chargeIds = cashItems.Select(i => i.ChargeTransactionId).ToList();
            var charges = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t => chargeIds.Contains(t.PaymentTransactionId) && t.OrderId == orderId)
                .ToDictionaryAsync(t => t.PaymentTransactionId, cancellationToken);
            foreach (var item in cashItems)
            {
                var charge = charges[item.ChargeTransactionId];
                var refund = await paymentRefunds.ReserveManualCashAsync(
                    charge,
                    item.Amount,
                    note,
                    cancellationToken
                );
                reservations.Add(
                    new OrderRefundReservation(
                        refund.PaymentTransactionId,
                        charge.PaymentTransactionId,
                        charge.Method,
                        refund.Amount,
                        refund.Status
                    )
                );
            }
        }

        return reservations;
    }

    // Pha 2 (sau commit): lỗi cổng không làm hỏng việc nhả đơn; khoản hoàn giữ Pending để job quét thử lại.
    private async Task ExecuteGatewayRefundsAsync(
        int orderId,
        IReadOnlyList<OrderRefundReservation> reservations
    )
    {
        var pending = reservations
            .Where(r => r.Method.IsGatewayMethod() && r.Status == PaymentStatus.Pending)
            .ToList();
        if (pending.Count == 0)
            return;

        try
        {
            await orderRefunds.ExecuteAsync(pending, false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "OrderRefundExecuteAfterReleaseFailed OrderId {OrderId} RefundTransactionIds {RefundIds}",
                orderId,
                string.Join(",", pending.Select(r => r.RefundTransactionId))
            );
        }
    }
}
