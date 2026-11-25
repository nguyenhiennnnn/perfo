using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Support;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Support;

public class SupportRequestService(IAppDbContext db, IAppCache cache) : ISupportRequestService
{
    public async Task<OffsetPageResult<SupportRequestListItemDto>> GetForCustomerAsync(
        int userId,
        SupportRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.SupportRequests.AsNoTracking().Where(r => r.UserId == userId);
        if (status is not null)
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.LastMessageAt)
            .ThenByDescending(r => r.SupportRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new SupportRequestListItemDto(
                r.SupportRequestId,
                r.OrderId,
                r.Order.OrderCode,
                r.Type,
                r.Subject,
                r.Status,
                r.DueAt,
                r.CreatedAt,
                r.LastMessageAt,
                r.ResolvedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<SupportRequestListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<SupportRequestDto> GetForCustomerAsync(
        int userId,
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .SupportRequests.AsNoTracking()
                .Include(r => r.Order)
                .Include(r => r.AssignedToUser)
                .Include(r => r.Messages)
                .ThenInclude(m => m.AuthorUser)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    r => r.SupportRequestId == requestId && r.UserId == userId,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_SupportRequest);

        return SupportRequestMapper.ToDto(
            request,
            request.Order.OrderCode,
            request.AssignedToUser?.FullName,
            request.Messages
        );
    }

    public async Task<SupportRequestDto> CreateForCustomerAsync(
        int userId,
        CreateSupportRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == request.OrderId)
                .Select(o => new { o.OrderId, o.OrderCode, o.UserId })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var openCount = await db.SupportRequests.CountAsync(
            r => r.OrderId == order.OrderId && r.Status != SupportRequestStatus.Resolved,
            cancellationToken
        );
        if (openCount >= SupportRequestLimits.MaxOpenPerOrder)
            throw new BusinessRuleException(
                string.Format(
                    Messages.Rule_SupportRequestTooManyOpen,
                    SupportRequestLimits.MaxOpenPerOrder
                )
            );

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var entity = new SupportRequest
        {
            OrderId = order.OrderId,
            UserId = userId,
            Type = request.Type,
            Subject = request.Subject.Trim(),
            Status = SupportRequestStatus.New,
            DueAt = now.AddHours(settings.SupportRequestDueHours),
            CreatedAt = now,
            UpdatedAt = now,
            LastMessageAt = now,
            Messages =
            [
                new SupportMessage
                {
                    AuthorType = SupportMessageAuthorType.Customer,
                    AuthorUserId = userId,
                    Content = request.Content.Trim(),
                    CreatedAt = now,
                },
            ],
        };
        db.SupportRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return SupportRequestMapper.ToDto(entity, order.OrderCode, null, entity.Messages);
    }

    public async Task<SupportRequestDto> ReplyForCustomerAsync(
        int userId,
        int requestId,
        ReplySupportRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity =
            await db.SupportRequests.FirstOrDefaultAsync(
                r => r.SupportRequestId == requestId && r.UserId == userId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_SupportRequest);
        if (entity.Status == SupportRequestStatus.Resolved)
            throw new BusinessRuleException(Messages.Rule_SupportRequestResolved);

        var now = DateTime.UtcNow;
        db.SupportMessages.Add(
            new SupportMessage
            {
                SupportRequestId = entity.SupportRequestId,
                AuthorType = SupportMessageAuthorType.Customer,
                AuthorUserId = userId,
                Content = request.Content.Trim(),
                CreatedAt = now,
            }
        );
        entity.LastMessageAt = now;
        entity.UpdatedAt = now;
        if (entity.Status == SupportRequestStatus.WaitingCustomer)
            entity.Status = SupportRequestStatus.InProgress;
        await db.SaveChangesAsync(cancellationToken);

        return await GetForCustomerAsync(userId, requestId, cancellationToken);
    }
}
