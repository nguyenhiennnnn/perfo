using System.Globalization;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Support;
using Cinema.Application.Options;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Support;

public class SupportRequestStaffService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    INotificationService notificationService,
    IOrderTicketEmailSender ticketEmailSender,
    IOptions<PromotionOptions> promotionOptions,
    IOptions<AppUrlOptions> appUrlOptions
) : ISupportRequestStaffService
{
    public async Task<OffsetPageResult<StaffSupportRequestListItemDto>> GetAsync(
        SupportRequestStaffListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);
        var now = DateTime.UtcNow;

        var query = db.SupportRequests.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(r => r.Status == status);
        if (filter.Unassigned == true)
            query = query.Where(r => r.AssignedToUserId == null);
        else if (filter.AssigneeId is { } assigneeId)
            query = query.Where(r => r.AssignedToUserId == assigneeId);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(r =>
                    EF.Functions.ILike(r.Order.OrderCode, pattern)
                    || EF.Functions.ILike(r.Subject, pattern)
                    || (r.User != null && EF.Functions.ILike(r.User.FullName, pattern))
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Status == SupportRequestStatus.Resolved ? 1 : 0)
            .ThenBy(r => r.DueAt)
            .ThenByDescending(r => r.SupportRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new StaffSupportRequestListItemDto(
                r.SupportRequestId,
                r.OrderId,
                r.Order.OrderCode,
                r.UserId,
                r.User != null ? r.User.FullName : null,
                r.Type,
                r.Subject,
                r.Status,
                r.AssignedToUserId,
                r.AssignedToUser != null ? r.AssignedToUser.FullName : null,
                r.DueAt,
                r.Status != SupportRequestStatus.Resolved && r.DueAt < now,
                r.CreatedAt,
                r.LastMessageAt,
                r.ResolvedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<StaffSupportRequestListItemDto>.Create(
            items,
            page,
            pageSize,
            total
        );
    }

    public async Task<StaffSupportRequestDto> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .SupportRequests.AsNoTracking()
                .Include(r => r.Order)
                .Include(r => r.User)
                .Include(r => r.AssignedToUser)
                .Include(r => r.ApologyPromotion)
                .Include(r => r.Messages)
                .ThenInclude(m => m.AuthorUser)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.SupportRequestId == requestId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_SupportRequest);

        return SupportRequestStaffMapper.ToDto(request, DateTime.UtcNow);
    }

    public async Task<StaffSupportRequestDto> AssignAsync(
        int requestId,
        AssignSupportRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindTrackedAsync(requestId, cancellationToken);
        EnsureNotResolved(entity);

        if (request.AssigneeId is { } assigneeId)
        {
            var valid = await db.Users.AnyAsync(
                u =>
                    u.UserId == assigneeId
                    && u.IsActive
                    && (u.Role.RoleName == Roles.Admin || u.Role.RoleName == Roles.Employee),
                cancellationToken
            );
            if (!valid)
                throw new ValidationAppException(
                    Messages.Common_InvalidData,
                    new Dictionary<string, string[]>
                    {
                        ["assigneeId"] = [Messages.Invalid_SupportRequest_Assignee],
                    }
                );
        }

        var before = AuditSnapshots.SupportRequestState(entity);
        var previousStatus = entity.Status;
        var now = DateTime.UtcNow;
        entity.AssignedToUserId = request.AssigneeId;
        if (request.AssigneeId is not null && entity.Status == SupportRequestStatus.New)
            entity.Status = SupportRequestStatus.InProgress;
        entity.UpdatedAt = now;

        auditLogger.Record(
            AuditActions.AssignSupportRequest,
            AuditEntityTypes.SupportRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.SupportRequestState(entity)
        );
        await SaveAsync(cancellationToken);

        if (entity.Status != previousStatus)
            NotifyCustomer(entity, true, null, null);
        return await GetByIdAsync(requestId, cancellationToken);
    }

    public async Task<StaffSupportRequestDto> ChangeStatusAsync(
        int requestId,
        ChangeSupportRequestStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindTrackedAsync(requestId, cancellationToken);
        if (entity.Status == request.Status)
            throw new BusinessRuleException(Messages.Rule_SupportRequestStatusUnchanged);

        var before = AuditSnapshots.SupportRequestState(entity);
        var now = DateTime.UtcNow;
        ApplyStatus(entity, request.Status, now);
        entity.UpdatedAt = now;

        auditLogger.Record(
            AuditActions.ChangeSupportRequestStatus,
            AuditEntityTypes.SupportRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.SupportRequestState(entity)
        );
        await SaveAsync(cancellationToken);

        NotifyCustomer(entity, true, null, null);
        return await GetByIdAsync(requestId, cancellationToken);
    }

    public async Task<StaffSupportRequestDto> ReplyAsync(
        int requestId,
        StaffReplySupportRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindTrackedAsync(requestId, cancellationToken);
        EnsureNotResolved(entity);

        var content = request.Content.Trim();
        var now = DateTime.UtcNow;
        var before = AuditSnapshots.SupportRequestState(entity);
        var previousStatus = entity.Status;
        var target =
            request.Status
            ?? (previousStatus == SupportRequestStatus.New ? SupportRequestStatus.InProgress : previousStatus);

        db.SupportMessages.Add(
            new SupportMessage
            {
                SupportRequestId = entity.SupportRequestId,
                AuthorType = SupportMessageAuthorType.Staff,
                AuthorUserId = actorAccessor.Current?.UserId,
                Content = content,
                CreatedAt = now,
            }
        );
        ApplyStatus(entity, target, now);
        entity.LastMessageAt = now;
        entity.UpdatedAt = now;

        if (target != previousStatus)
            auditLogger.Record(
                AuditActions.ChangeSupportRequestStatus,
                AuditEntityTypes.SupportRequest,
                requestId.ToString(),
                before,
                AuditSnapshots.SupportRequestState(entity)
            );
        await SaveAsync(cancellationToken);

        NotifyCustomer(entity, target != previousStatus, content, null);
        return await GetByIdAsync(requestId, cancellationToken);
    }

    public async Task<StaffSupportRequestDto> IssueApologyCodeAsync(
        int requestId,
        IssueSupportApologyCodeRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindTrackedAsync(requestId, cancellationToken);
        EnsureNotResolved(entity);
        var customerId = RequireCustomer(entity);
        if (entity.ApologyPromotionId is not null)
            throw new ConflictAppException(Messages.Conflict_SupportApologyCodeIssued);

        var now = DateTime.UtcNow;
        var validDays = request.ValidDays ?? promotionOptions.Value.PersonalCodeValidDays;
        var promotion = SupportApologyPromotionFactory.Build(
            await SupportApologyPromotionFactory.NewCodeAsync(db, cancellationToken),
            string.Format(Messages.Support_ApologyPromotionName, entity.Order.OrderCode),
            request,
            customerId,
            now,
            validDays
        );

        var before = AuditSnapshots.SupportRequestState(entity);
        var previousStatus = entity.Status;
        entity.ApologyPromotion = promotion;
        if (entity.Status == SupportRequestStatus.New)
            entity.Status = SupportRequestStatus.InProgress;
        db.SupportMessages.Add(
            new SupportMessage
            {
                SupportRequestId = entity.SupportRequestId,
                AuthorType = SupportMessageAuthorType.System,
                AuthorUserId = actorAccessor.Current?.UserId,
                Content = string.Format(
                    Messages.Support_MessageApologyCodeIssued,
                    promotion.Code,
                    BusinessTime
                        .ToLocal(promotion.EndsAt!.Value)
                        .ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture)
                ),
                CreatedAt = now,
            }
        );
        entity.LastMessageAt = now;
        entity.UpdatedAt = now;

        auditLogger.Record(
            AuditActions.IssueSupportApologyCode,
            AuditEntityTypes.SupportRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.SupportApologyCodeState(entity, promotion)
        );
        await SaveAsync(cancellationToken);

        NotifyCustomer(
            entity,
            entity.Status != previousStatus,
            null,
            new SupportApologyCodeEmailData(
                promotion.Code,
                promotion.DiscountType,
                promotion.DiscountValue,
                promotion.MaxDiscountAmount,
                promotion.EndsAt!.Value
            )
        );
        return await GetByIdAsync(requestId, cancellationToken);
    }

    public async Task<StaffSupportRequestDto> ResendTicketEmailAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindTrackedAsync(requestId, cancellationToken);
        EnsureNotResolved(entity);
        RequireCustomer(entity);
        if (entity.Order.Status != OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_SupportTicketEmailOrderNotConfirmed);

        if (!await ticketEmailSender.SendAsync(entity.OrderId, cancellationToken))
            throw new BusinessRuleException(Messages.Rule_SupportTicketEmailUnavailable);

        var now = DateTime.UtcNow;
        if (entity.Status == SupportRequestStatus.New)
            entity.Status = SupportRequestStatus.InProgress;
        db.SupportMessages.Add(
            new SupportMessage
            {
                SupportRequestId = entity.SupportRequestId,
                AuthorType = SupportMessageAuthorType.System,
                AuthorUserId = actorAccessor.Current?.UserId,
                Content = string.Format(
                    Messages.Support_MessageTicketEmailResent,
                    entity.Order.OrderCode
                ),
                CreatedAt = now,
            }
        );
        entity.LastMessageAt = now;
        entity.UpdatedAt = now;

        auditLogger.Record(
            AuditActions.ResendTicketEmail,
            AuditEntityTypes.SupportRequest,
            requestId.ToString(),
            null,
            new { entity.OrderId, entity.Order.OrderCode }
        );
        await SaveAsync(cancellationToken);

        return await GetByIdAsync(requestId, cancellationToken);
    }

    private async Task<SupportRequest> FindTrackedAsync(
        int requestId,
        CancellationToken cancellationToken
    ) =>
        await db
            .SupportRequests.Include(r => r.Order)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.SupportRequestId == requestId, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_SupportRequest);

    private static void EnsureNotResolved(SupportRequest entity)
    {
        if (entity.Status == SupportRequestStatus.Resolved)
            throw new BusinessRuleException(Messages.Rule_SupportRequestResolvedStaff);
    }

    private static int RequireCustomer(SupportRequest entity) =>
        entity.UserId is { } customerId && entity.User is not null
            ? customerId
            : throw new BusinessRuleException(Messages.Rule_SupportRequestNoCustomer);

    private static void ApplyStatus(SupportRequest entity, SupportRequestStatus status, DateTime now)
    {
        if (entity.Status == status)
            return;

        entity.Status = status;
        entity.ResolvedAt = status == SupportRequestStatus.Resolved ? now : null;
    }

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

    private void NotifyCustomer(
        SupportRequest entity,
        bool statusChanged,
        string? staffMessage,
        SupportApologyCodeEmailData? apologyCode
    )
    {
        if (entity.User is null)
            return;

        notificationService.SendSupportRequestUpdated(
            entity.User.Email,
            new SupportRequestUpdateEmailData(
                entity.User.FullName,
                entity.SupportRequestId,
                entity.Subject,
                entity.Order.OrderCode,
                entity.Status,
                statusChanged,
                staffMessage,
                apologyCode,
                $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/account/tickets/{entity.OrderId}"
            )
        );
    }
}
