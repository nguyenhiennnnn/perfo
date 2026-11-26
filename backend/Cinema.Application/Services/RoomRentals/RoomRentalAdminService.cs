using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalAdminService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    IRoomRentalCancellationService cancellationService
) : IRoomRentalAdminService
{
    public async Task<OffsetPageResult<RoomRentalAdminListItemDto>> GetAsync(
        RoomRentalAdminListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.RoomRentalRequests.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(r =>
                r.RequestCode.ToLower().Contains(term)
                || r.FullName.ToLower().Contains(term)
                || (r.OrganizationName != null && r.OrganizationName.ToLower().Contains(term))
                || r.ContactPhone.Contains(term)
                || r.ContactEmail.Contains(term)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RoomRentalRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoomRentalAdminListItemDto(
                r.RoomRentalRequestId,
                r.RequestCode,
                r.FullName,
                r.OrganizationName,
                r.AudienceType,
                r.ContactPhone,
                r.DesiredStartAt,
                r.AttendeeCount,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<RoomRentalAdminListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<RoomRentalAdminDto> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var entity =
            await db
                .RoomRentalRequests.AsNoTracking()
                .Include(r => r.Movie)
                .Include(r => r.ScreenType)
                .FirstOrDefaultAsync(r => r.RoomRentalRequestId == requestId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);
        return ToDto(entity);
    }

    public async Task<RoomRentalAdminDto> ChangeStatusAsync(
        int requestId,
        ChangeRoomRentalRequestStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity =
            await db
                .RoomRentalRequests.Include(r => r.Movie)
                .Include(r => r.ScreenType)
                .FirstOrDefaultAsync(r => r.RoomRentalRequestId == requestId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        if (entity.Status == request.Status)
            throw new BusinessRuleException(Messages.Rule_RoomRentalStatusUnchanged);
        if (!RoomRentalStatusRules.CanChange(entity.Status, request.Status))
            throw new BusinessRuleException(Messages.Rule_RoomRentalStatusTransitionInvalid);

        if (
            request.Status == RoomRentalRequestStatus.Cancelled
            && RoomRentalCancellationRules.IsPaymentStage(entity.Status)
        )
        {
            await cancellationService.CancelAsync(
                requestId,
                RoomRentalCancelKind.Customer,
                request.Note,
                cancellationToken
            );
            return await GetByIdAsync(requestId, cancellationToken);
        }

        var before = AuditSnapshots.RoomRentalRequestState(entity);
        entity.Status = request.Status;
        entity.StatusNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        auditLogger.Record(
            AuditActions.ChangeRoomRentalRequestStatus,
            AuditEntityTypes.RoomRentalRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.RoomRentalRequestState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    private static RoomRentalAdminDto ToDto(RoomRentalRequest r) =>
        new(
            r.RoomRentalRequestId,
            r.RequestCode,
            r.FullName,
            r.OrganizationName,
            r.AudienceType,
            r.ContactPhone,
            r.ContactEmail,
            r.DesiredStartAt,
            r.FlexibleFromAt,
            r.FlexibleToAt,
            r.AttendeeCount,
            r.MovieId,
            r.Movie?.Title,
            r.BringsOwnContent,
            r.ContentNote,
            r.ScreenTypeId,
            r.ScreenType?.Name,
            r.ComboRequest,
            r.Notes,
            r.Status,
            r.StatusNote,
            RoomRentalStatusRules.AllowedNext(r.Status),
            r.CreatedAt,
            r.UpdatedAt
        );
}
