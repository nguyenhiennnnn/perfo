using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipPlanService(IAppDbContext db, IAuditLogger auditLogger)
    : IMembershipPlanService
{
    public async Task<OffsetPageResult<MembershipPlanDto>> GetAdminAsync(
        int page,
        int pageSize,
        MembershipPlanListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.MembershipPlans.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(p => p.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(p => EF.Functions.ILike(p.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.MembershipPlanId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<MembershipPlanDto>.Create(
            items.Select(MembershipPlanMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<MembershipPlanDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        MembershipPlanMapper.ToDto(
            await db
                .MembershipPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembershipPlanId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_MembershipPlan)
        );

    public async Task<IReadOnlyList<MembershipPlanDto>> GetActiveAsync(
        CancellationToken cancellationToken
    )
    {
        var plans = await db
            .MembershipPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Period)
            .ThenBy(p => p.Price)
            .ThenBy(p => p.MembershipPlanId)
            .ToListAsync(cancellationToken);
        return plans.Select(MembershipPlanMapper.ToDto).ToList();
    }

    public async Task<MembershipPlanDto> CreateAsync(
        UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    )
    {
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(0, name, cancellationToken);

        var entity = new MembershipPlan { CreatedAt = DateTime.UtcNow };
        MembershipPlanMapper.Apply(entity, name, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.MembershipPlans.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.MembershipPlan,
            entity.MembershipPlanId.ToString(),
            null,
            AuditSnapshots.MembershipPlanState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MembershipPlanMapper.ToDto(entity);
    }

    public async Task<MembershipPlanDto> UpdateAsync(
        int id,
        UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(id, name, cancellationToken);

        var before = AuditSnapshots.MembershipPlanState(entity);
        MembershipPlanMapper.Apply(entity, name, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.MembershipPlan,
            entity.MembershipPlanId.ToString(),
            before,
            AuditSnapshots.MembershipPlanState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return MembershipPlanMapper.ToDto(entity);
    }

    public async Task<DeleteMembershipPlanResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.MembershipPlanState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.MembershipPlan,
            entity.MembershipPlanId.ToString(),
            before,
            AuditSnapshots.MembershipPlanState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteMembershipPlanResponse(id, true);
    }

    private async Task<MembershipPlan> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.MembershipPlans.FirstOrDefaultAsync(
            p => p.MembershipPlanId == id,
            cancellationToken
        ) ?? throw new NotFoundException(Messages.NotFound_MembershipPlan);

    private async Task EnsureNameAvailableAsync(
        int currentId,
        string name,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.MembershipPlans.AnyAsync(
            p => p.Name == name && p.MembershipPlanId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_MembershipPlanNameExists);
    }
}
