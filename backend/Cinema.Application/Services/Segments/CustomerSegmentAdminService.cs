using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Segments;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Segments;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Segments;

public class CustomerSegmentAdminService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICustomerSegmentQueryService queryService
) : ICustomerSegmentAdminService
{
    public async Task<OffsetPageResult<CustomerSegmentDto>> GetAsync(
        int page,
        int pageSize,
        CustomerSegmentListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.CustomerSegments.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(s => s.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(s =>
                    EF.Functions.ILike(s.Name, pattern)
                    || (s.Description != null && EF.Functions.ILike(s.Description, pattern))
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.SegmentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<CustomerSegmentDto>(entities.Count);
        foreach (var entity in entities)
            items.Add(await ToDtoAsync(entity, cancellationToken));

        return OffsetPageResult<CustomerSegmentDto>.Create(items, page, pageSize, total);
    }

    public async Task<CustomerSegmentDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    ) =>
        await ToDtoAsync(
            await db
                .CustomerSegments.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SegmentId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_CustomerSegment),
            cancellationToken
        );

    public async Task<CustomerSegmentDto> CreateAsync(
        UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    )
    {
        await EnsureNameAvailableAsync(request.Name, null, cancellationToken);

        var entity = new CustomerSegment { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.CustomerSegments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.CustomerSegment,
            entity.SegmentId.ToString(),
            null,
            AuditSnapshots.CustomerSegmentState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<CustomerSegmentDto> UpdateAsync(
        int id,
        UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureNameAvailableAsync(request.Name, id, cancellationToken);

        var before = AuditSnapshots.CustomerSegmentState(entity);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.CustomerSegment,
            entity.SegmentId.ToString(),
            before,
            AuditSnapshots.CustomerSegmentState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DeleteCustomerSegmentResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.CustomerSegmentState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.CustomerSegment,
            entity.SegmentId.ToString(),
            before,
            AuditSnapshots.CustomerSegmentState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteCustomerSegmentResponse(id, true);
    }

    private async Task<CustomerSegment> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.CustomerSegments.FirstOrDefaultAsync(s => s.SegmentId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_CustomerSegment);

    private async Task EnsureNameAvailableAsync(
        string name,
        int? excludeId,
        CancellationToken cancellationToken
    )
    {
        var normalized = name.Trim().ToLower();
        var exists = await db
            .CustomerSegments.AsNoTracking()
            .AnyAsync(
                s => s.Name.ToLower() == normalized && (excludeId == null || s.SegmentId != excludeId),
                cancellationToken
            );
        if (exists)
            throw new ConflictAppException(Messages.Rule_CustomerSegmentNameExists);
    }

    private static void Apply(CustomerSegment entity, UpsertCustomerSegmentRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.ConditionJson = SegmentConditionCompiler.Serialize(request.Conditions);
        entity.IsActive = request.IsActive;
    }

    private async Task<CustomerSegmentDto> ToDtoAsync(
        CustomerSegment entity,
        CancellationToken cancellationToken
    )
    {
        var conditions = SegmentConditionCompiler.Deserialize(entity.ConditionJson);
        var (customers, eligible) = await queryService.CountAsync(conditions, cancellationToken);
        return new CustomerSegmentDto(
            entity.SegmentId,
            entity.Name,
            entity.Description,
            conditions,
            entity.IsActive,
            customers,
            eligible,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
