using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Pricing;

public class PricingRuleService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : IPricingRuleService
{
    public async Task<OffsetPageResult<PricingRuleDto>> GetAdminAsync(
        int page,
        int pageSize,
        PricingRuleListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.PricingRules.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(r => r.IsActive == isActive);
        if (EnumConverters.ParseNullableEnum<PricingRuleType>(filter.RuleType) is { } ruleType)
            query = query.Where(r => r.RuleType == ruleType);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(r => EF.Functions.ILike(r.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.PricingRuleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<PricingRuleDto>.Create(
            items.Select(PricingMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<PricingRuleDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        PricingMapper.ToDto(
            await db
                .PricingRules.AsNoTracking()
                .FirstOrDefaultAsync(r => r.PricingRuleId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_PricingRule)
        );

    public async Task<PricingRuleDto> CreateAsync(
        UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        var name = request.Name.Trim();
        var condition = ResolveCondition(request, out var type);
        await EnsureValidAsync(0, name, request, condition, cancellationToken);

        var entity = new PricingRule { CreatedAt = DateTime.UtcNow };
        Apply(entity, name, type, condition, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.PricingRules.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.PricingRule,
            entity.PricingRuleId.ToString(),
            null,
            AuditSnapshots.PricingRuleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.PricingRules, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<PricingRuleDto> UpdateAsync(
        int id,
        UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        var condition = ResolveCondition(request, out var type);
        await EnsureValidAsync(id, name, request, condition, cancellationToken);

        var before = AuditSnapshots.PricingRuleState(entity);
        Apply(entity, name, type, condition, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.PricingRule,
            entity.PricingRuleId.ToString(),
            before,
            AuditSnapshots.PricingRuleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.PricingRules, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<DeletePricingEntityResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.PricingRuleState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.PricingRule,
            entity.PricingRuleId.ToString(),
            before,
            AuditSnapshots.PricingRuleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.PricingRules, cancellationToken);
        return new DeletePricingEntityResponse(id, true);
    }

    private async Task<PricingRule> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.PricingRules.FirstOrDefaultAsync(r => r.PricingRuleId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_PricingRule);

    private static PricingRuleCondition ResolveCondition(
        UpsertPricingRuleRequest request,
        out PricingRuleType type
    )
    {
        type = EnumConverters.ParseEnum<PricingRuleType>(request.RuleType, "ruleType");
        var dto =
            request.Condition
            ?? throw new ValidationAppException(Messages.Validation_PricingRule_ConditionRequired);
        return PricingMapper.ToCondition(type, dto);
    }

    private async Task EnsureValidAsync(
        int currentId,
        string name,
        UpsertPricingRuleRequest request,
        PricingRuleCondition condition,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.PricingRules.AnyAsync(
            r => r.Name == name && r.PricingRuleId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_PricingRuleExists);

        var errors = new Dictionary<string, string[]>();

        var roomIds = PricingMapper.NormalizeIds(request.RoomIds);
        if (
            roomIds.Count > 0
            && await db.CinemaRooms.CountAsync(r => roomIds.Contains(r.RoomId), cancellationToken)
                != roomIds.Count
        )
            errors["roomIds"] = [Messages.Invalid_PricingRule_ScopeNotFound];

        var screenTypeIds = PricingMapper.NormalizeIds(request.ScreenTypeIds);
        if (
            screenTypeIds.Count > 0
            && await db.ScreenTypes.CountAsync(
                s => screenTypeIds.Contains(s.ScreenTypeId),
                cancellationToken
            ) != screenTypeIds.Count
        )
            errors["screenTypeIds"] = [Messages.Invalid_PricingRule_ScopeNotFound];

        var movieIds = PricingMapper.NormalizeIds(request.MovieIds);
        if (
            movieIds.Count > 0
            && await db.Movies.CountAsync(m => movieIds.Contains(m.MovieId), cancellationToken)
                != movieIds.Count
        )
            errors["movieIds"] = [Messages.Invalid_PricingRule_ScopeNotFound];

        var specialDayIds = condition.SpecialDayIds ?? [];
        if (
            specialDayIds.Count > 0
            && await db.SpecialDays.CountAsync(
                d => specialDayIds.Contains(d.SpecialDayId),
                cancellationToken
            ) != specialDayIds.Count
        )
            errors["condition.specialDayIds"] = [Messages.Invalid_PricingRule_SpecialDayNotFound];

        if (errors.Count > 0)
            throw new ValidationAppException(Messages.Common_InvalidData, errors);
    }

    private static void Apply(
        PricingRule entity,
        string name,
        PricingRuleType type,
        PricingRuleCondition condition,
        UpsertPricingRuleRequest request
    )
    {
        entity.Name = name;
        entity.RuleType = type;
        entity.ConditionJson = condition.ToJson();
        entity.AdjustmentMode = EnumConverters.ParseEnum<PricingAdjustmentMode>(
            request.AdjustmentMode,
            "adjustmentMode"
        );
        entity.AdjustmentValue = type == PricingRuleType.Occupancy ? null : request.AdjustmentValue;
        entity.Priority = request.Priority;
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.RoomIds = PricingMapper.NormalizeIds(request.RoomIds);
        entity.ScreenTypeIds = PricingMapper.NormalizeIds(request.ScreenTypeIds);
        entity.MovieIds = PricingMapper.NormalizeIds(request.MovieIds);
        entity.IsActive = request.IsActive;
    }
}
