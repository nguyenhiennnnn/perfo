using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Support;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Support;

public class ExceptionalRefundService(
    IAppDbContext db,
    IAppCache cache,
    IPaymentLedgerService ledger,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    IOrderRefundService orderRefunds,
    IRealtimeNotifier realtimeNotifier,
    ILogger<ExceptionalRefundService> logger
) : IExceptionalRefundService
{
    private static readonly NumberFormatInfo MoneyFormat = new() { NumberGroupSeparator = "." };

    public async Task<OffsetPageResult<ExceptionalRefundProposalDto>> GetAsync(
        ExceptionalRefundProposalListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.ExceptionalRefundProposals.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(p => p.Status == status);
        if (filter.OrderId is { } orderId)
            query = query.Where(p => p.OrderId == orderId);
        if (filter.ProposedByUserId is { } proposedBy)
            query = query.Where(p => p.ProposedByUserId == proposedBy);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Status == ExceptionalRefundStatus.Pending ? 0 : 1)
            .ThenByDescending(p => p.ProposedAt)
            .ThenByDescending(p => p.ExceptionalRefundProposalId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<ExceptionalRefundProposalDto>.Create(items, page, pageSize, total);
    }

    public async Task<ExceptionalRefundProposalDto> GetByIdAsync(
        int proposalId,
        CancellationToken cancellationToken
    ) =>
        await db
            .ExceptionalRefundProposals.AsNoTracking()
            .Where(p => p.ExceptionalRefundProposalId == proposalId)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_ExceptionalRefundProposal);

    public async Task<ExceptionalRefundProposalDto> ProposeAsync(
        CreateExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    )
    {
        var actor = RequireActor();
        var amount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero);
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        var orderExists = await db.Orders.AnyAsync(o => o.OrderId == request.OrderId, cancellationToken);
        if (!orderExists)
            throw new NotFoundException(Messages.NotFound_Order);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockOrderAsync(request.OrderId, cancellationToken);

        var summary = await ledger.GetSummaryAsync(request.OrderId, cancellationToken);
        var openProposals = await db
            .ExceptionalRefundProposals.AsNoTracking()
            .Where(p => p.OrderId == request.OrderId && p.Status == ExceptionalRefundStatus.Pending)
            .Select(p => new { p.OrderLineId, p.Amount })
            .ToListAsync(cancellationToken);

        var refundable = summary.NetPaidAmount - summary.PendingRefundAmount - openProposals.Sum(p => p.Amount);
        if (request.OrderLineId is { } lineId)
        {
            var line =
                await db
                    .OrderLines.AsNoTracking()
                    .Where(l => l.OrderLineId == lineId && l.OrderId == request.OrderId)
                    .Select(l => new { l.PaidAmount, l.RefundedAmount })
                    .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationAppException(
                    Messages.Common_InvalidData,
                    new Dictionary<string, string[]>
                    {
                        ["orderLineId"] = [Messages.Invalid_ExceptionalRefund_OrderLine],
                    }
                );
            var lineRefundable =
                line.PaidAmount
                - line.RefundedAmount
                - openProposals.Where(p => p.OrderLineId == lineId).Sum(p => p.Amount);
            refundable = Math.Min(refundable, lineRefundable);
        }

        refundable = Math.Max(refundable, 0m);
        if (amount > refundable)
            throw ExceedsRefundable(refundable);

        var now = DateTime.UtcNow;
        var entity = new ExceptionalRefundProposal
        {
            OrderId = request.OrderId,
            OrderLineId = request.OrderLineId,
            Amount = amount,
            Reason = request.Reason.Trim(),
            Status = ExceptionalRefundStatus.Pending,
            RequiresAdminApproval = amount > settings.ExceptionalRefundApprovalThreshold,
            ApprovalThreshold = settings.ExceptionalRefundApprovalThreshold,
            ProposedByUserId = actor.UserId,
            ProposedAt = now,
        };
        db.ExceptionalRefundProposals.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        auditLogger.Record(
            AuditActions.ProposeExceptionalRefund,
            AuditEntityTypes.ExceptionalRefundProposal,
            entity.ExceptionalRefundProposalId.ToString(),
            null,
            new
            {
                entity.OrderId,
                entity.OrderLineId,
                entity.Amount,
                entity.Reason,
                entity.Status,
                entity.RequiresAdminApproval,
                entity.ApprovalThreshold,
                entity.ProposedByUserId,
            }
        );
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(entity.ExceptionalRefundProposalId, cancellationToken);
    }

    public async Task<ExceptionalRefundProposalDto> ApproveAsync(
        int proposalId,
        ApproveExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    )
    {
        var actor = RequireAdmin();
        var entity =
            await db
                .ExceptionalRefundProposals.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ExceptionalRefundProposalId == proposalId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ExceptionalRefundProposal);
        EnsurePending(entity);
        if (entity.ProposedByUserId == actor.UserId)
            throw new ForbiddenAppException(Messages.Forbidden_ExceptionalRefundSelfApprove);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (entity.RequiresAdminApproval && note is null)
            throw new ValidationAppException(
                Messages.Common_InvalidData,
                new Dictionary<string, string[]>
                {
                    ["note"] = [Messages.Validation_ExceptionalRefund_ApprovalNoteRequired],
                }
            );
        var cancelLineIds = (request.CancelTicketLineIds ?? []).Distinct().ToList();

        IReadOnlyList<ReleasedSeat> releasedSeats;
        IReadOnlyList<OrderRefundReservation> reservations;
        IReadOnlyList<Cinema.Application.Interfaces.Payments.PaymentRefundResult> results;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockOrderAsync(entity.OrderId, cancellationToken);

        var now = DateTime.UtcNow;
        var claimed = await db
            .ExceptionalRefundProposals.Where(p =>
                p.ExceptionalRefundProposalId == proposalId && p.Status == ExceptionalRefundStatus.Pending
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(p => p.Status, ExceptionalRefundStatus.Approved)
                        .SetProperty(p => p.ReviewedByUserId, (int?)actor.UserId)
                        .SetProperty(p => p.ReviewedAt, (DateTime?)now)
                        .SetProperty(p => p.ReviewNote, note),
                cancellationToken
            );
        if (claimed == 0)
            throw new BusinessRuleException(Messages.Rule_ExceptionalRefundNotPending);

        var summary = await ledger.GetSummaryAsync(entity.OrderId, cancellationToken);
        var refundable = Math.Max(summary.NetPaidAmount - summary.PendingRefundAmount, 0m);
        if (entity.Amount > refundable)
            throw ExceedsRefundable(refundable);

        if (entity.OrderLineId is { } lineId)
        {
            var amount = entity.Amount;
            var updated = await db
                .OrderLines.Where(l =>
                    l.OrderLineId == lineId
                    && l.OrderId == entity.OrderId
                    && l.RefundedAmount + amount <= l.PaidAmount
                )
                .ExecuteUpdateAsync(
                    s => s.SetProperty(l => l.RefundedAmount, l => l.RefundedAmount + amount),
                    cancellationToken
                );
            if (updated == 0)
                throw ExceedsRefundable(0m);
        }

        var plan = await orderRefunds.BuildPlanForAmountAsync(entity.OrderId, entity.Amount, cancellationToken);
        reservations = await orderRefunds.ReserveAsync(
            plan,
            actor.UserId,
            string.Format(Messages.Refund_ExceptionalDescription, proposalId),
            cancellationToken
        );
        results = await orderRefunds.ExecuteAsync(reservations, cancellationToken);

        var gatewayIds = reservations
            .Where(r => r.Method.IsGatewayMethod())
            .Select(r => r.RefundTransactionId)
            .ToHashSet();
        var gatewayResults = results.Where(r => gatewayIds.Contains(r.RefundTransactionId)).ToList();
        if (gatewayResults.Count > 0 && gatewayResults.All(r => r.IsFailed))
        {
            var message =
                gatewayResults.Select(r => r.Message).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? Messages.Refund_ExecutionError;
            logger.LogWarning(
                "ExceptionalRefundFailed ProposalId {ProposalId} OrderId {OrderId} Message {Message}",
                proposalId,
                entity.OrderId,
                message
            );
            throw new BusinessRuleException(string.Format(Messages.Rule_RefundFailed, message));
        }

        var gatewayRefunded = gatewayResults.Where(r => r.Success).ToList();
        try
        {
            var failedIds = results.Where(r => r.IsFailed).Select(r => r.RefundTransactionId).ToList();
            await MarkManualRefundsAsync(entity.OrderId, reservations, failedIds, cancellationToken);

            releasedSeats =
                cancelLineIds.Count > 0
                    ? await CancelTicketsAsync(entity.OrderId, cancelLineIds, cancellationToken)
                    : [];

            auditLogger.Record(
                AuditActions.ApproveExceptionalRefund,
                AuditEntityTypes.ExceptionalRefundProposal,
                proposalId.ToString(),
                new { Status = ExceptionalRefundStatus.Pending },
                new
                {
                    Status = ExceptionalRefundStatus.Approved,
                    entity.OrderId,
                    entity.OrderLineId,
                    entity.Amount,
                    entity.Reason,
                    entity.RequiresAdminApproval,
                    entity.ProposedByUserId,
                    ReviewedByUserId = actor.UserId,
                    ReviewNote = note,
                    Refunded = results.Where(r => r.Success).Sum(r => r.Amount),
                    ManualRefund = results.Where(r => !r.Success).Sum(r => r.Amount),
                    CancelledTicketLineIds = cancelLineIds,
                }
            );
            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (gatewayRefunded.Count > 0)
        {
            logger.LogCritical(
                ex,
                "ExceptionalRefundCommitFailed ProposalId {ProposalId} RefundTransactionIds {RefundIds} — đã hoàn tiền ở cổng nhưng DB chưa ghi nhận.",
                proposalId,
                string.Join(",", gatewayRefunded.Select(r => r.RefundTransactionId))
            );
            throw;
        }

        foreach (var group in releasedSeats.GroupBy(s => s.ShowtimeId))
            await realtimeNotifier.NotifySeatReleasedAsync(
                group.Key,
                [.. group.Select(s => s.ShowtimeSeatId)],
                cancellationToken
            );

        return await GetByIdAsync(proposalId, cancellationToken);
    }

    private async Task<IReadOnlyList<ReleasedSeat>> CancelTicketsAsync(
        int orderId,
        List<int> lineIds,
        CancellationToken cancellationToken
    )
    {
        var tickets = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.OrderId == orderId
                && lineIds.Contains(l.OrderLineId)
                && l.LineType == OrderLineType.Ticket
                && l.Quantity > l.RefundedQuantity
                && !l.IsCheckedIn
            )
            .Select(l => new { l.OrderLineId, l.ShowtimeSeatId })
            .ToListAsync(cancellationToken);
        if (tickets.Count != lineIds.Count)
            throw new BusinessRuleException(Messages.Rule_ExceptionalRefundTicketNotCancellable);

        var cancelled = await db
            .OrderLines.Where(l =>
                lineIds.Contains(l.OrderLineId) && l.OrderId == orderId && l.Quantity > l.RefundedQuantity
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.RefundedQuantity, l => l.Quantity),
                cancellationToken
            );
        if (cancelled != lineIds.Count)
            throw new ConflictAppException(Messages.Refund_LineChanged);

        var seatIds = tickets.Where(t => t.ShowtimeSeatId != null).Select(t => t.ShowtimeSeatId!.Value).ToList();
        if (seatIds.Count == 0)
            return [];

        var seats = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => seatIds.Contains(ss.ShowtimeSeatId) && ss.Status == ShowtimeSeatStatus.Booked)
            .Select(ss => new ReleasedSeat(ss.ShowtimeId, ss.ShowtimeSeatId))
            .ToListAsync(cancellationToken);
        if (seats.Count == 0)
            return [];

        var releasable = seats.Select(s => s.ShowtimeSeatId).ToList();
        await db
            .ShowtimeSeats.Where(ss =>
                releasable.Contains(ss.ShowtimeSeatId) && ss.Status == ShowtimeSeatStatus.Booked
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                        .SetProperty(x => x.HeldByUserId, (int?)null)
                        .SetProperty(x => x.HeldByOrderId, (int?)null)
                        .SetProperty(x => x.HeldUntil, (DateTime?)null),
                cancellationToken
            );
        return seats;
    }

    private async Task MarkManualRefundsAsync(
        int orderId,
        IReadOnlyList<OrderRefundReservation> reservations,
        IReadOnlyList<int> failedRefundIds,
        CancellationToken cancellationToken
    )
    {
        var chargeIds = reservations
            .Where(r => failedRefundIds.Contains(r.RefundTransactionId))
            .Select(r => r.ChargeTransactionId)
            .Distinct()
            .ToList();
        if (chargeIds.Count == 0)
            return;

        await db
            .PaymentTransactions.Where(t => chargeIds.Contains(t.PaymentTransactionId))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.NeedsManualRefund, true), cancellationToken);
        logger.LogCritical(
            "ExceptionalRefundNeedsManual OrderId {OrderId} ChargeTransactionIds {ChargeIds} — cần hoàn tiền thủ công.",
            orderId,
            string.Join(",", chargeIds)
        );
    }

    private static BusinessRuleException ExceedsRefundable(decimal refundable) =>
        new(
            string.Format(
                Messages.Rule_ExceptionalRefundExceedsRefundable,
                refundable.ToString("N0", MoneyFormat)
            )
        );

    public async Task<ExceptionalRefundProposalDto> RejectAsync(
        int proposalId,
        RejectExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    )
    {
        var actor = RequireAdmin();
        var entity = await FindTrackedAsync(proposalId, cancellationToken);
        EnsurePending(entity);

        var before = new { entity.Status, entity.ReviewedByUserId };
        entity.Status = ExceptionalRefundStatus.Rejected;
        entity.ReviewedByUserId = actor.UserId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNote = request.Reason.Trim();

        auditLogger.Record(
            AuditActions.RejectExceptionalRefund,
            AuditEntityTypes.ExceptionalRefundProposal,
            proposalId.ToString(),
            before,
            new
            {
                entity.Status,
                entity.Amount,
                entity.ProposedByUserId,
                entity.ReviewedByUserId,
                entity.ReviewNote,
            }
        );
        await SaveAsync(cancellationToken);
        return await GetByIdAsync(proposalId, cancellationToken);
    }

    private ActorContext RequireActor() =>
        actorAccessor.Current ?? throw new ForbiddenAppException(Messages.Forbidden_AccessDenied);

    private ActorContext RequireAdmin()
    {
        var actor = RequireActor();
        if (!actor.IsAdmin)
            throw new ForbiddenAppException(Messages.Forbidden_ExceptionalRefundAdminOnly);
        return actor;
    }

    private static void EnsurePending(ExceptionalRefundProposal entity)
    {
        if (entity.Status != ExceptionalRefundStatus.Pending)
            throw new BusinessRuleException(Messages.Rule_ExceptionalRefundNotPending);
    }

    private async Task<ExceptionalRefundProposal> FindTrackedAsync(
        int proposalId,
        CancellationToken cancellationToken
    ) =>
        await db.ExceptionalRefundProposals.FirstOrDefaultAsync(
            p => p.ExceptionalRefundProposalId == proposalId,
            cancellationToken
        ) ?? throw new NotFoundException(Messages.NotFound_ExceptionalRefundProposal);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException(Messages.Db_ConcurrencyConflict);
        }
    }

    private static readonly System.Linq.Expressions.Expression<
        Func<ExceptionalRefundProposal, ExceptionalRefundProposalDto>
    > ToDtoExpression = p => new ExceptionalRefundProposalDto(
        p.ExceptionalRefundProposalId,
        p.OrderId,
        p.Order.OrderCode,
        p.OrderLineId,
        p.OrderLine != null ? p.OrderLine.Description : null,
        p.Amount,
        p.Reason,
        p.Status,
        p.RequiresAdminApproval,
        p.ApprovalThreshold,
        p.ProposedByUserId,
        p.ProposedByUser.FullName,
        p.ProposedAt,
        p.ReviewedByUserId,
        p.ReviewedByUser != null ? p.ReviewedByUser.FullName : null,
        p.ReviewedAt,
        p.ReviewNote
    );
}
