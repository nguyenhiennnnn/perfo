using Cinema.Application.DTOs.Audit;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Audit;

public class AuditLogQueryService(IAppDbContext db) : IAuditLogQueryService
{
    public async Task<OffsetPageResult<AuditLogListItemDto>> GetAsync(
        AuditLogListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.AuditLogs.AsNoTracking().ApplyFilter(filter);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.AuditLogId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListItemDto(
                a.AuditLogId,
                a.OccurredAt,
                a.ActorUserId,
                db.Users.Where(u => u.UserId == a.ActorUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefault(),
                a.Action,
                a.EntityType,
                a.EntityId
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<AuditLogListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<AuditLogDetailDto> GetDetailAsync(
        long id,
        CancellationToken cancellationToken
    ) =>
        await db
            .AuditLogs.AsNoTracking()
            .Where(a => a.AuditLogId == id)
            .Select(a => new AuditLogDetailDto(
                a.AuditLogId,
                a.OccurredAt,
                a.ActorUserId,
                db.Users.Where(u => u.UserId == a.ActorUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefault(),
                db.Users.Where(u => u.UserId == a.ActorUserId)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                a.Action,
                a.EntityType,
                a.EntityId,
                a.BeforeJson,
                a.AfterJson
            ))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_AuditLog);

    public async Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync(
        CancellationToken cancellationToken
    )
    {
        var entityTypes = await db
            .AuditLogs.Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);

        var actorIds = db
            .AuditLogs.Where(a => a.ActorUserId != null)
            .Select(a => a.ActorUserId!.Value)
            .Distinct();
        var actors = await db
            .Users.Where(u => actorIds.Contains(u.UserId))
            .OrderBy(u => u.FullName)
            .Select(u => new AuditLogActorDto(u.UserId, u.FullName))
            .ToListAsync(cancellationToken);

        return new AuditLogFilterOptionsDto(entityTypes, actors);
    }
}
