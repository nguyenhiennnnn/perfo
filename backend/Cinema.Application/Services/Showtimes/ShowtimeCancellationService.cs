using Cinema.Application.BackgroundJobs;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeCancellationService(
    IAppDbContext db,
    IOrderReleaseService releaseService,
    IOrderRefundService orderRefunds,
    IPaymentRefundService paymentRefunds,
    IOrderCancellationFinalizer finalizer,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ShowtimeCancellationService> logger
) : IShowtimeCancellationService
{
    public Task<DeleteShowtimeResponse> DeleteShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken
    ) => DeleteShowtimeAsync(showtimeId, CancelReason.ShowtimeCancelled, cancellationToken);

    public async Task<DeleteShowtimeResponse> DeleteShowtimeAsync(
        int showtimeId,
        CancelReason reason,
        CancellationToken cancellationToken
    )
    {
        if (reason is not (CancelReason.ShowtimeCancelled or CancelReason.RoomClosed))
            throw new ValidationAppException(
                Messages.Rule_ShowtimeCancelReasonNotAllowed,
                new Dictionary<string, string[]>
                {
                    ["reason"] = [Messages.Rule_ShowtimeCancelReasonNotAllowed],
                }
            );

        var showtime =
            await db
                .Showtimes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        ShowtimeRules.EnsureScheduled(showtime, Messages.Common_ActionCancel);
        if (showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ShowtimeStartedCannotCancel);

        var adminUserId = actorAccessor.Current?.UserId;
        var gatewayOrderIds = new List<int>();
        var cashOrderCount = 0;
        var cashTotal = 0m;
        var cancelledCount = 0;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var claimed = await db
                .Showtimes.Where(s =>
                    s.ShowtimeId == showtimeId && s.Status == ShowtimeStatus.Scheduled
                )
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.Status, ShowtimeStatus.Cancelled),
                    cancellationToken
                );
            if (claimed == 0)
                throw new BusinessRuleException(Messages.Rule_ShowtimeCancelled);

            foreach (var orderId in await LoadAffectedOrderIdsAsync(showtimeId, cancellationToken))
            {
                await db.LockOrderAsync(orderId, cancellationToken);
                var order = await db
                    .Orders.AsNoTracking()
                    .Where(o => o.OrderId == orderId)
                    .Select(o => new { o.OrderCode, o.Status })
                    .FirstAsync(cancellationToken);

                if (order.Status == OrderStatus.Pending)
                {
                    if (
                        await releaseService.CancelPendingAsync(
                            orderId,
                            reason,
                            CancelledBy.Admin,
                            cancellationToken
                        )
                    )
                        cancelledCount++;
                    continue;
                }

                if (order.Status != OrderStatus.Confirmed)
                    continue;

                var outcome = await CancelConfirmedAsync(
                    orderId,
                    order.OrderCode,
                    adminUserId,
                    reason,
                    cancellationToken
                );
                cancelledCount++;
                if (outcome.HasGatewayRefund)
                    gatewayOrderIds.Add(orderId);
                if (outcome.CashAmount > 0m)
                {
                    cashOrderCount++;
                    cashTotal += outcome.CashAmount;
                }
            }

            var heldSeatIds = await db
                .ShowtimeSeats.Where(ss =>
                    ss.ShowtimeId == showtimeId && ss.Status == ShowtimeSeatStatus.Held
                )
                .Select(ss => ss.ShowtimeSeatId)
                .ToListAsync(cancellationToken);

            await db
                .ShowtimeSeats.Where(ss =>
                    ss.ShowtimeId == showtimeId && ss.Status == ShowtimeSeatStatus.Held
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                            .SetProperty(x => x.HeldByUserId, (int?)null)
                            .SetProperty(x => x.HeldByOrderId, (int?)null)
                            .SetProperty(x => x.HeldUntil, (DateTime?)null),
                    cancellationToken
                );

            await SeatHoldEventRecorder.StageAsync(
                db,
                heldSeatIds,
                SeatHoldEventType.Released,
                DateTime.UtcNow,
                cancellationToken
            );

            auditLogger.Record(
                AuditActions.Cancel,
                AuditEntityTypes.Showtime,
                showtimeId.ToString(),
                AuditSnapshots.ShowtimeBeforeCancel(),
                AuditSnapshots.ShowtimeAfterCancel(
                    cancelledCount,
                    gatewayOrderIds.Count,
                    cashOrderCount,
                    cashTotal,
                    reason
                )
            );

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var orderId in gatewayOrderIds)
        {
            try
            {
                backgroundJobClient.Enqueue<ShowtimeRefundJob>(j =>
                    j.RunAsync(orderId, null, CancellationToken.None)
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "ShowtimeRefundEnqueueFailed ShowtimeId {ShowtimeId} OrderId {OrderId}",
                    showtimeId,
                    orderId
                );
            }
        }

        logger.LogInformation(
            "ShowtimeCancelled ShowtimeId {ShowtimeId} OrdersCancelled {OrderCount} AutoRefundQueued {QueuedCount} ManualRefundNeeded {ManualCount}",
            showtimeId,
            cancelledCount,
            gatewayOrderIds.Count,
            cashOrderCount
        );

        return new DeleteShowtimeResponse(
            showtimeId,
            "Cancelled",
            cashOrderCount,
            gatewayOrderIds.Count
        );
    }

    private async Task<List<int>> LoadAffectedOrderIdsAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var ticketOrderIds = db
            .OrderLines.Where(l => l.ShowtimeId == showtimeId && l.LineType == OrderLineType.Ticket)
            .Select(l => l.OrderId);

        return await db
            .Orders.AsNoTracking()
            .Where(o =>
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
                && (
                    ticketOrderIds.Contains(o.OrderId)
                    || (o.OriginalOrderId != null && ticketOrderIds.Contains(o.OriginalOrderId.Value))
                )
            )
            .OrderBy(o => o.OrderId)
            .Select(o => o.OrderId)
            .ToListAsync(cancellationToken);
    }

    private async Task<ConfirmedOrderOutcome> CancelConfirmedAsync(
        int orderId,
        string orderCode,
        int? adminUserId,
        CancelReason reason,
        CancellationToken cancellationToken
    )
    {
        var plan = await orderRefunds.BuildPlanAsync(
            orderId,
            RefundScope.Full(false),
            cancellationToken
        );
        var note = string.Format(Messages.Refund_ShowtimeCancelledDescription, orderCode);

        var cashItems = plan.Items.Where(i => i.Method == PaymentMethod.Cash).ToList();
        var otherItems = plan.Items.Where(i => i.Method != PaymentMethod.Cash).ToList();

        var hasGatewayRefund = false;
        if (otherItems.Count > 0)
        {
            var reservations = await orderRefunds.ReserveAsync(
                new OrderRefundPlan(
                    orderId,
                    otherItems.Sum(i => i.Amount),
                    plan.FulfilledComboExcluded,
                    otherItems
                ),
                adminUserId,
                note,
                cancellationToken
            );
            hasGatewayRefund = reservations.Any(r =>
                r.Method.IsGatewayMethod() && r.Status == PaymentStatus.Pending
            );
        }

        if (cashItems.Count > 0)
        {
            var chargeIds = cashItems.Select(i => i.ChargeTransactionId).ToList();
            var charges = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t => chargeIds.Contains(t.PaymentTransactionId) && t.OrderId == orderId)
                .ToDictionaryAsync(t => t.PaymentTransactionId, cancellationToken);

            foreach (var item in cashItems)
                await paymentRefunds.ReserveManualCashAsync(
                    charges[item.ChargeTransactionId],
                    item.Amount,
                    note,
                    cancellationToken
                );
        }

        await finalizer.FinalizeAsync(
            orderId,
            reason,
            CancelledBy.Admin,
            plan,
            cancellationToken
        );

        return new ConfirmedOrderOutcome(hasGatewayRefund, cashItems.Sum(i => i.Amount));
    }

    private sealed record ConfirmedOrderOutcome(bool HasGatewayRefund, decimal CashAmount);
}
