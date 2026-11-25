using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Ledger;

public class LedgerAdjustmentService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : ILedgerAdjustmentService
{
    public async Task<OffsetPageResult<LedgerAdjustmentDto>> GetAdjustmentsAsync(
        LedgerAdjustmentListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.LedgerAdjustments.AsNoTracking();
        if (filter.PostingFrom is { } postingFrom)
            query = query.Where(a => a.PostingDate >= postingFrom);
        if (filter.PostingTo is { } postingTo)
            query = query.Where(a => a.PostingDate <= postingTo);
        if (filter.OriginalDate is { } originalDate)
            query = query.Where(a => a.OriginalDate == originalDate);
        if (
            !string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<LedgerAdjustmentType>(filter.Type.Trim(), true, out var type)
        )
            query = query.Where(a => a.Type == type);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.PostingDate)
            .ThenByDescending(a => a.LedgerAdjustmentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                Adjustment = a,
                CreatedByName = a.CreatedByUser != null ? a.CreatedByUser.FullName : null,
            })
            .ToListAsync(cancellationToken);

        return OffsetPageResult<LedgerAdjustmentDto>.Create(
            rows.Select(r => ToDto(r.Adjustment, r.CreatedByName)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<LedgerAdjustmentDto> CreateManualAsync(
        int userId,
        CreateLedgerAdjustmentRequest request,
        CancellationToken cancellationToken
    )
    {
        if (
            !await db
                .DailyClosings.AsNoTracking()
                .AnyAsync(
                    c =>
                        c.BusinessDate == request.OriginalDate
                        && c.Status == DailyClosingStatus.Closed,
                    cancellationToken
                )
        )
            throw new BusinessRuleException(Messages.Rule_LedgerAdjustmentDayNotClosed);

        var orderId = request.OrderId;
        if (request.PaymentTransactionId is { } transactionId)
        {
            var transactionOrderId = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t => t.PaymentTransactionId == transactionId)
                .Select(t => (int?)t.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_Payment);
            if (orderId is not null && orderId != transactionOrderId)
                throw new BusinessRuleException(Messages.Rule_LedgerAdjustmentReferenceMismatch);
            orderId = transactionOrderId;
        }
        else if (
            orderId is { } id
            && !await db.Orders.AsNoTracking().AnyAsync(o => o.OrderId == id, cancellationToken)
        )
            throw new NotFoundException(Messages.NotFound_Order);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var postingDate = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        if (postingDate <= request.OriginalDate)
            throw new BusinessRuleException(Messages.Rule_LedgerAdjustmentDayNotClosed);

        var adjustment = new LedgerAdjustmentInput(
            request.OriginalDate,
            LedgerAdjustmentType.Manual,
            request.Reason.Trim(),
            request.TicketRevenueDelta,
            request.ComboRevenueDelta,
            request.GiftCardSoldDelta,
            request.DiscountDelta,
            request.RefundAmountDelta,
            request.CancelledOrdersDelta,
            orderId,
            request.PaymentTransactionId,
            CreatedByUserId: userId
        ).ToEntity(postingDate, now);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.LedgerAdjustments.Add(adjustment);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.CreateLedgerAdjustment,
            AuditEntityTypes.LedgerAdjustment,
            adjustment.LedgerAdjustmentId.ToString(),
            null,
            AuditJson.ToObject(
                new
                {
                    adjustment.PostingDate,
                    adjustment.OriginalDate,
                    adjustment.Type,
                    adjustment.TicketRevenueDelta,
                    adjustment.ComboRevenueDelta,
                    adjustment.GiftCardSoldDelta,
                    adjustment.DiscountDelta,
                    adjustment.RefundAmountDelta,
                    adjustment.CancelledOrdersDelta,
                    adjustment.Reason,
                    adjustment.OrderId,
                    adjustment.PaymentTransactionId,
                }
            )
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdByName = await db
            .Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        return ToDto(adjustment, createdByName);
    }

    private static LedgerAdjustmentDto ToDto(LedgerAdjustment a, string? createdByName) =>
        new(
            a.LedgerAdjustmentId,
            a.PostingDate,
            a.OriginalDate,
            a.Type.ToString(),
            a.TicketRevenueDelta,
            a.ComboRevenueDelta,
            a.GiftCardSoldDelta,
            a.DiscountDelta,
            a.RefundAmountDelta,
            a.CancelledOrdersDelta,
            a.Reason,
            a.CreatedByUserId,
            createdByName,
            a.CreatedAt,
            a.OrderId,
            a.PaymentTransactionId
        );
}
