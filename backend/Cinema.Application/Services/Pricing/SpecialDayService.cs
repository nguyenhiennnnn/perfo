using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Catalog;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Pricing;

public class SpecialDayService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : ISpecialDayService
{
    public async Task<OffsetPageResult<SpecialDayDto>> GetAdminAsync(
        int page,
        int pageSize,
        SpecialDayListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.SpecialDays.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(d => d.IsActive == isActive);
        if (filter.Year is { } year)
        {
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);
            query = query.Where(d => d.StartDate <= yearEnd && d.EndDate >= yearStart);
        }
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(d => EF.Functions.ILike(d.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.StartDate)
            .ThenBy(d => d.SpecialDayId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<SpecialDayDto>.Create(
            items.Select(PricingMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<SpecialDayDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        PricingMapper.ToDto(
            await db
                .SpecialDays.AsNoTracking()
                .FirstOrDefaultAsync(d => d.SpecialDayId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_SpecialDay)
        );

    public async Task<SpecialDayDto> CreateAsync(
        UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    )
    {
        var name = request.Name.Trim();
        await EnsureNotDuplicatedAsync(0, name, request.StartDate, cancellationToken);

        var entity = new SpecialDay { CreatedAt = DateTime.UtcNow };
        Apply(entity, name, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.SpecialDays.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.SpecialDay,
            entity.SpecialDayId.ToString(),
            null,
            AuditSnapshots.SpecialDayState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.HolidayCalendar, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<SpecialDayDto> UpdateAsync(
        int id,
        UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNotDuplicatedAsync(id, name, request.StartDate, cancellationToken);

        var before = AuditSnapshots.SpecialDayState(entity);
        Apply(entity, name, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.SpecialDay,
            entity.SpecialDayId.ToString(),
            before,
            AuditSnapshots.SpecialDayState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.HolidayCalendar, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<DeletePricingEntityResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        await EnsureNotReferencedAsync(id, cancellationToken);

        var before = AuditSnapshots.SpecialDayState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.SpecialDay,
            entity.SpecialDayId.ToString(),
            before,
            AuditSnapshots.SpecialDayState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.HolidayCalendar, cancellationToken);
        return new DeletePricingEntityResponse(id, true);
    }

    private async Task<SpecialDay> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.SpecialDays.FirstOrDefaultAsync(d => d.SpecialDayId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_SpecialDay);

    private async Task EnsureNotDuplicatedAsync(
        int currentId,
        string name,
        DateOnly startDate,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.SpecialDays.AnyAsync(
            d => d.Name == name && d.StartDate == startDate && d.SpecialDayId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_SpecialDayExists);
    }

    private async Task EnsureNotReferencedAsync(int id, CancellationToken cancellationToken)
    {
        var conditions = await db
            .PricingRules.AsNoTracking()
            .Where(r => r.RuleType == PricingRuleType.SpecialDay)
            .Select(r => r.ConditionJson)
            .ToListAsync(cancellationToken);

        var referenced = conditions.Any(json =>
            PricingRuleCondition.TryParse(json)?.SpecialDayIds?.Contains(id) == true
        );
        if (referenced)
            throw new ConflictAppException(Messages.Conflict_SpecialDayInUse);
    }

    private static void Apply(SpecialDay entity, string name, UpsertSpecialDayRequest request)
    {
        entity.Name = name;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.IsActive = request.IsActive;
    }
}
