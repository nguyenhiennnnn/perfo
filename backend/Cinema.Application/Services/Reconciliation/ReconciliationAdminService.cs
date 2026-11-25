using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Reconciliation;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Application.Options;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Reconciliation;

public class ReconciliationAdminService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    IOptions<BookingOptions> bookingOptions
) : IReconciliationAdminService
{
    private readonly BookingOptions _options = bookingOptions.Value;

    public async Task<OffsetPageResult<ReconciliationSessionDto>> GetSessionsAsync(
        ReconciliationSessionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        if (filter.FromDate is { } rangeFrom && filter.ToDate is { } rangeTo && rangeFrom > rangeTo)
            throw new ValidationAppException(
                Messages.Validation_DateRangeTo_BeforeFrom,
                new Dictionary<string, string[]> { ["toDate"] = [Messages.Validation_DateRangeTo_BeforeFrom] }
            );

        (page, pageSize) = Normalize(page, pageSize);

        var query = db.ReconciliationSessions.AsNoTracking();
        if (filter.Gateway is { } gateway)
            query = query.Where(s => s.Gateway == gateway);
        if (filter.FromDate is { } from)
            query = query.Where(s => s.PeriodTo >= from);
        if (filter.ToDate is { } to)
            query = query.Where(s => s.PeriodFrom <= to);

        var total = await query.CountAsync(cancellationToken);
        var rows = await SessionRows(query.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.ReconciliationSessionId))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<ReconciliationSessionDto>.Create(
            rows.Select(ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<ReconciliationSessionDto> GetSessionAsync(
        int sessionId,
        CancellationToken cancellationToken
    )
    {
        var row = await SessionRows(
                db.ReconciliationSessions.AsNoTracking().Where(s => s.ReconciliationSessionId == sessionId)
            )
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? throw new NotFoundException(Messages.NotFound_ReconciliationSession)
            : ToDto(row);
    }

    public async Task<OffsetPageResult<ReconciliationItemDto>> GetItemsAsync(
        ReconciliationItemListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        (page, pageSize) = Normalize(page, pageSize);

        var query = db.ReconciliationItems.AsNoTracking();
        if (filter.SessionId is { } sessionId)
            query = query.Where(i => i.ReconciliationSessionId == sessionId);
        if (filter.Gateway is { } gateway)
            query = query.Where(i => i.Gateway == gateway);
        if (filter.Resolution is { } resolution)
            query = query.Where(i => i.Resolution == resolution);
        if (filter.AssignedToUserId is { } assignedTo)
            query = query.Where(i => i.AssignedToUserId == assignedTo);
        query = filter.Result is { } result
            ? query.Where(i => i.Result == result)
            : query.Where(i => i.Result != ReconciliationResult.Matched);

        var total = await query.CountAsync(cancellationToken);
        var rows = await ItemRows(query.OrderByDescending(i => i.ReconciliationItemId))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<ReconciliationItemDto>.Create(
            rows.Select(ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<ReconciliationItemDto> UpdateItemAsync(
        int itemId,
        UpdateReconciliationItemRequest request,
        int actorUserId,
        CancellationToken cancellationToken
    )
    {
        var item =
            await db.ReconciliationItems.FirstOrDefaultAsync(
                i => i.ReconciliationItemId == itemId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_ReconciliationItem);

        if (item.Result == ReconciliationResult.Matched)
            throw new BusinessRuleException(Messages.Rule_ReconciliationItemMatched);

        if (request.AssignedToUserId is { } assignee && assignee != item.AssignedToUserId)
            await EnsureAssigneeAllowedAsync(assignee, cancellationToken);

        var before = Snapshot(item);

        if (request.Note is not null)
        {
            var note = request.Note.Trim();
            item.Note = note.Length == 0 ? null : note;
        }

        if (request.ClearAssignee)
            item.AssignedToUserId = null;
        else if (request.AssignedToUserId is { } newAssignee)
            item.AssignedToUserId = newAssignee;

        if (request.Resolution is { } resolution && resolution != item.Resolution)
        {
            item.Resolution = resolution;
            item.ResolvedAt = resolution == ReconciliationResolution.Open ? null : DateTime.UtcNow;
            item.ResolvedByUserId = resolution == ReconciliationResolution.Open ? null : actorUserId;
        }

        var after = Snapshot(item);
        if (before != after)
        {
            auditLogger.Record(
                AuditActions.Update,
                AuditEntityTypes.ReconciliationItem,
                item.ReconciliationItemId.ToString(),
                before,
                after
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        var row = await ItemRows(
                db.ReconciliationItems.AsNoTracking().Where(i => i.ReconciliationItemId == itemId)
            )
            .FirstAsync(cancellationToken);
        return ToDto(row);
    }

    private async Task EnsureAssigneeAllowedAsync(int userId, CancellationToken cancellationToken)
    {
        var allowed = await db.Users.AnyAsync(
            u =>
                u.UserId == userId
                && u.IsActive
                && (u.Role.RoleName == Roles.Admin || u.Role.RoleName == Roles.Employee),
            cancellationToken
        );
        if (!allowed)
            throw new ValidationAppException(
                Messages.Invalid_Reconciliation_AssigneeNotAllowed,
                new Dictionary<string, string[]>
                {
                    ["assignedToUserId"] = [Messages.Invalid_Reconciliation_AssigneeNotAllowed],
                }
            );
    }

    private (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (
            Math.Max(1, page),
            pageSize <= 0 ? _options.DefaultPageSize : Math.Min(pageSize, _options.MaxPageSize)
        );

    private static ItemState Snapshot(ReconciliationItem item) =>
        new(
            item.Note,
            item.AssignedToUserId,
            item.Resolution.ToString(),
            item.ResolvedAt,
            item.ResolvedByUserId
        );

    private static IQueryable<SessionRow> SessionRows(IQueryable<ReconciliationSession> query) =>
        query.Select(s => new SessionRow(
            s.ReconciliationSessionId,
            s.Gateway,
            s.Source,
            s.FileName,
            s.PeriodFrom,
            s.PeriodTo,
            s.Status,
            s.TotalRows,
            s.MatchedCount,
            s.MissingInSystemCount,
            s.MissingAtGatewayCount,
            s.AmountMismatchCount,
            s.StatusMismatchCount,
            s.PaidAtGatewayOrderUnconfirmedCount,
            s.GatewayTotalAmount,
            s.SystemTotalAmount,
            s.Items.Count(i =>
                i.Result != ReconciliationResult.Matched && i.Resolution == ReconciliationResolution.Open
            ),
            s.Items.Count(i =>
                i.Result != ReconciliationResult.Matched && i.Resolution == ReconciliationResolution.Resolved
            ),
            s.Items.Count(i =>
                i.Result != ReconciliationResult.Matched && i.Resolution == ReconciliationResolution.Ignored
            ),
            s.ErrorMessage,
            s.CreatedByUserId,
            s.CreatedByUser.FullName,
            s.CreatedAt,
            s.CompletedAt
        ));

    private static IQueryable<ItemRow> ItemRows(IQueryable<ReconciliationItem> query) =>
        query.Select(i => new ItemRow(
            i.ReconciliationItemId,
            i.ReconciliationSessionId,
            i.Gateway,
            i.GatewayTransactionCode,
            i.Amount,
            i.GatewayFee,
            i.GatewayStatus,
            i.GatewayTime,
            i.PaymentTransactionId,
            i.PaymentTransaction == null ? null : i.PaymentTransaction.TransactionCode,
            i.PaymentTransaction == null ? null : (int?)i.PaymentTransaction.OrderId,
            i.PaymentTransaction == null ? null : i.PaymentTransaction.Order.OrderCode,
            i.PaymentTransaction == null ? null : (decimal?)i.PaymentTransaction.Amount,
            i.Result,
            i.Resolution,
            i.Note,
            i.AssignedToUserId,
            i.AssignedToUser == null ? null : i.AssignedToUser.FullName,
            i.ResolvedAt,
            i.ResolvedByUserId,
            i.ResolvedByUser == null ? null : i.ResolvedByUser.FullName
        ));

    private static ReconciliationSessionDto ToDto(SessionRow r) =>
        new(
            r.Id,
            r.Gateway.ToString(),
            r.Source.ToString(),
            r.FileName,
            r.PeriodFrom,
            r.PeriodTo,
            r.Status.ToString(),
            r.TotalRows,
            r.MatchedCount,
            r.MissingInSystemCount,
            r.MissingAtGatewayCount,
            r.AmountMismatchCount,
            r.StatusMismatchCount,
            r.PaidAtGatewayOrderUnconfirmedCount,
            r.GatewayTotalAmount,
            r.SystemTotalAmount,
            r.OpenItemCount,
            r.ResolvedItemCount,
            r.IgnoredItemCount,
            r.ErrorMessage,
            r.CreatedByUserId,
            r.CreatedByName,
            r.CreatedAt,
            r.CompletedAt
        );

    private static ReconciliationItemDto ToDto(ItemRow r) =>
        new(
            r.Id,
            r.SessionId,
            r.Gateway.ToString(),
            r.Code,
            r.GatewayAmount,
            r.GatewayFee,
            r.GatewayStatus,
            r.GatewayTime,
            r.PaymentTransactionId,
            r.PaymentTransactionCode,
            r.OrderId,
            r.OrderCode,
            r.SystemAmount,
            r.Result.ToString(),
            r.Resolution.ToString(),
            r.Note,
            r.AssignedToUserId,
            r.AssignedToName,
            r.ResolvedAt,
            r.ResolvedByUserId,
            r.ResolvedByName
        );

    private sealed record ItemState(
        string? Note,
        int? AssignedToUserId,
        string Resolution,
        DateTime? ResolvedAt,
        int? ResolvedByUserId
    );

    private sealed record SessionRow(
        int Id,
        ReconciliationGateway Gateway,
        ReconciliationSource Source,
        string? FileName,
        DateOnly? PeriodFrom,
        DateOnly? PeriodTo,
        ReconciliationStatus Status,
        int TotalRows,
        int MatchedCount,
        int MissingInSystemCount,
        int MissingAtGatewayCount,
        int AmountMismatchCount,
        int StatusMismatchCount,
        int PaidAtGatewayOrderUnconfirmedCount,
        decimal GatewayTotalAmount,
        decimal SystemTotalAmount,
        int OpenItemCount,
        int ResolvedItemCount,
        int IgnoredItemCount,
        string? ErrorMessage,
        int CreatedByUserId,
        string? CreatedByName,
        DateTime CreatedAt,
        DateTime? CompletedAt
    );

    private sealed record ItemRow(
        int Id,
        int SessionId,
        ReconciliationGateway Gateway,
        string Code,
        decimal? GatewayAmount,
        decimal GatewayFee,
        string? GatewayStatus,
        DateTime? GatewayTime,
        int? PaymentTransactionId,
        string? PaymentTransactionCode,
        int? OrderId,
        string? OrderCode,
        decimal? SystemAmount,
        ReconciliationResult Result,
        ReconciliationResolution Resolution,
        string? Note,
        int? AssignedToUserId,
        string? AssignedToName,
        DateTime? ResolvedAt,
        int? ResolvedByUserId,
        string? ResolvedByName
    );
}
