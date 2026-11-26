using Cinema.Application.BackgroundJobs;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Services.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalCancellationService(
    IAppDbContext db,
    IOrderReleaseService releaseService,
    IOrderRefundService orderRefunds,
    IOrderCancellationFinalizer finalizer,
    IPaymentLedgerService ledger,
    ISeatBlockService seatBlockService,
    ISalesStatsService salesStats,
    ICurrentActorAccessor actorAccessor,
    IAuditLogger auditLogger,
    IBackgroundJobClient backgroundJobClient,
    ILogger<RoomRentalCancellationService> logger
) : IRoomRentalCancellationService
{
    public async Task<RoomRentalCancellationResultDto> CancelAsync(
        int requestId,
        RoomRentalCancelKind kind,
        string? note,
        CancellationToken cancellationToken
    )
    {
        if (kind == RoomRentalCancelKind.PaymentOverdue)
            throw new BusinessRuleException(Messages.Rule_RoomRentalCancelKindInvalid);

        var status =
            await db
                .RoomRentalRequests.AsNoTracking()
                .Where(r => r.RoomRentalRequestId == requestId)
                .Select(r => (RoomRentalRequestStatus?)r.Status)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);
        if (!RoomRentalCancellationRules.IsPaymentStage(status.Value))
            throw new BusinessRuleException(Messages.Rule_RoomRentalCancelStatusInvalid);

        var quote = await RoomRentalPaymentPlan.LoadLatestSentQuoteAsync(
            db,
            requestId,
            cancellationToken
        );
        if (quote.StartAt <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_RoomRentalCancelAfterStart);

        return await RunAsync(
                requestId,
                kind,
                string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                now =>
                    db.RoomRentalRequests.Where(r =>
                        RoomRentalCancellationRules.CancellableStatuses.Contains(r.Status)
                    ),
                cancellationToken
            )
            ?? throw new ConflictAppException(Messages.Conflict_RoomRentalCancelBeingProcessed);
    }

    public Task<RoomRentalCancellationResultDto?> TryCancelOverdueAsync(
        int requestId,
        CancellationToken cancellationToken
    ) =>
        RunAsync(
            requestId,
            RoomRentalCancelKind.PaymentOverdue,
            Messages.RoomRental_BalanceOverdueNote,
            now => RoomRentalCancellationRules.OverdueRequests(db, now),
            cancellationToken
        );

    private async Task<RoomRentalCancellationResultDto?> RunAsync(
        int requestId,
        RoomRentalCancelKind kind,
        string? statusNote,
        Func<DateTime, IQueryable<RoomRentalRequest>> claimable,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var request =
            await db
                .RoomRentalRequests.AsNoTracking()
                .Where(r => r.RoomRentalRequestId == requestId)
                .Select(r => new { r.RequestCode, r.Status, r.StatusNote })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);
        var quote = await RoomRentalPaymentPlan.LoadLatestSentQuoteAsync(
            db,
            requestId,
            cancellationToken
        );

        var depositPercent = RoomRentalCancellationRules.DepositRefundPercent(kind, quote, now);
        var reason = RoomRentalCancellationRules.ReasonOf(kind);
        var cancelledBy = RoomRentalCancellationRules.CancelledByOf(kind);
        var performedBy = actorAccessor.Current?.UserId;
        var refundNote = string.Format(Messages.Refund_RoomRentalCancelDescription, request.RequestCode);

        var gatewayOrderIds = new List<int>();
        var refunded = 0m;
        var retained = 0m;
        int? releasedShowtimeId = null;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var claimed = await claimable(now)
                .Where(r => r.RoomRentalRequestId == requestId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(r => r.Status, RoomRentalRequestStatus.Cancelled)
                            .SetProperty(r => r.StatusNote, statusNote)
                            .SetProperty(r => r.UpdatedAt, now),
                    cancellationToken
                );
            if (claimed == 0)
                return null;

            var orders = await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.RoomRentalRequestId == requestId
                    && o.Type == OrderType.RoomRental
                    && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
                )
                .OrderBy(o => o.OrderId)
                .Select(o => new { o.OrderId, o.RoomRentalPaymentStage })
                .ToListAsync(cancellationToken);

            foreach (var order in orders)
            {
                await db.LockOrderAsync(order.OrderId, cancellationToken);
                var status = await db
                    .Orders.AsNoTracking()
                    .Where(o => o.OrderId == order.OrderId)
                    .Select(o => o.Status)
                    .FirstAsync(cancellationToken);
                if (status is not (OrderStatus.Pending or OrderStatus.Confirmed))
                    continue;

                var percent =
                    order.RoomRentalPaymentStage == RoomRentalPaymentStage.Deposit
                        ? depositPercent
                        : 100;
                var netPaid = await ledger.GetNetPaidAmountAsync(order.OrderId, cancellationToken);
                var plan = new OrderRefundPlan(order.OrderId, 0m, 0m, []);
                var orderRefunded = 0m;

                if (percent > 0 && netPaid > 0m)
                {
                    plan = await orderRefunds.BuildPlanAsync(
                        order.OrderId,
                        percent >= 100
                            ? RefundScope.Full(false)
                            : new RefundScope(RefundScopeKind.Percent, percent, false),
                        cancellationToken
                    );
                    if (plan.TotalAmount > 0m && plan.Items.Count > 0)
                    {
                        var reservations = await orderRefunds.ReserveAsync(
                            plan,
                            performedBy,
                            refundNote,
                            cancellationToken
                        );
                        orderRefunded = reservations.Sum(r => r.Amount);
                        if (reservations.Any(r => r.Method.IsGatewayMethod()))
                            gatewayOrderIds.Add(order.OrderId);
                    }
                }

                refunded += orderRefunded;
                retained += Math.Max(0m, netPaid - orderRefunded);

                if (status == OrderStatus.Pending)
                {
                    if (
                        !await releaseService.CancelPendingAsync(
                            order.OrderId,
                            reason,
                            cancelledBy,
                            cancellationToken
                        )
                    )
                        throw new ConflictAppException(
                            Messages.Conflict_BookingBeingProcessedOrCancelled
                        );
                }
                else
                {
                    await finalizer.FinalizeAsync(
                        order.OrderId,
                        reason,
                        cancelledBy,
                        plan,
                        cancellationToken
                    );
                }
            }

            releasedShowtimeId = await ReleaseShowtimeAsync(requestId, cancellationToken);

            auditLogger.Record(
                AuditActions.CancelRoomRentalRequest,
                AuditEntityTypes.RoomRentalRequest,
                requestId.ToString(),
                AuditSnapshots.RoomRentalRequestState(
                    request.Status,
                    request.StatusNote,
                    now
                ),
                new
                {
                    Status = RoomRentalRequestStatus.Cancelled,
                    StatusNote = statusNote,
                    Kind = kind,
                    DepositRefundPercent = depositPercent,
                    RefundedAmount = refunded,
                    RetainedAmount = retained,
                    ReleasedShowtimeId = releasedShowtimeId,
                    HoursBeforeStart = Math.Round((quote.StartAt - now).TotalHours, 1),
                    PerformedByUserId = performedBy,
                }
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
                    "RoomRentalRefundEnqueueFailed RequestId {RequestId} OrderId {OrderId}",
                    requestId,
                    orderId
                );
            }
        }

        if (releasedShowtimeId is { } showtimeId)
            await salesStats.RefreshShowtimesAsync([showtimeId], cancellationToken);

        logger.LogInformation(
            "RoomRentalCancelled RequestId {RequestId} Kind {Kind} DepositRefundPercent {Percent} Refunded {Refunded} Retained {Retained} ShowtimeId {ShowtimeId}",
            requestId,
            kind,
            depositPercent,
            refunded,
            retained,
            releasedShowtimeId
        );

        return new RoomRentalCancellationResultDto(
            requestId,
            request.RequestCode,
            RoomRentalRequestStatus.Cancelled,
            kind,
            depositPercent,
            refunded,
            retained,
            releasedShowtimeId is not null
        );
    }

    private async Task<int?> ReleaseShowtimeAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var showtimeId = await db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.RoomRentalRequestId == requestId && s.Status == ShowtimeStatus.Scheduled
            )
            .Select(s => (int?)s.ShowtimeId)
            .FirstOrDefaultAsync(cancellationToken);
        if (showtimeId is not { } id)
            return null;

        var blockedSeatIds = await db
            .SeatBlocks.AsNoTracking()
            .Where(b => b.ShowtimeId == id && b.Type == SeatBlockType.RoomRental)
            .Select(b => b.ShowtimeSeatId)
            .ToListAsync(cancellationToken);
        foreach (var chunk in blockedSeatIds.Chunk(SeatBlockLimits.MaxSeatsPerRequest))
            await seatBlockService.UnblockSeatsAsync(
                id,
                chunk,
                [SeatBlockType.RoomRental],
                cancellationToken
            );

        await db
            .Showtimes.Where(s => s.ShowtimeId == id && s.Status == ShowtimeStatus.Scheduled)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.Status, ShowtimeStatus.Cancelled),
                cancellationToken
            );
        return id;
    }
}
