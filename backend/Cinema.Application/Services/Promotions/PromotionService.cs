using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : IPromotionService
{
    public async Task<OffsetPageResult<PromotionDto>> GetAdminAsync(
        int page,
        int pageSize,
        PromotionListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.Promotions.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(p => p.IsActive == isActive);
        if (EnumConverters.ParseNullableEnum<PromotionType>(filter.Type) is { } type)
            query = query.Where(p => p.Type == type);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(p =>
                    EF.Functions.ILike(p.Code, pattern)
                    || EF.Functions.ILike(p.Name, pattern)
                    || (
                        p.AssignedUser != null && EF.Functions.ILike(p.AssignedUser.Email, pattern)
                    )
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.PromotionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PromotionRow(
                p,
                p.AssignedUser == null ? null : p.AssignedUser.FullName,
                p.AssignedUser == null ? null : p.AssignedUser.Email
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<PromotionDto>.Create(
            rows.Select(PromotionMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<PromotionDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row =
            await db
                .Promotions.AsNoTracking()
                .Where(p => p.PromotionId == id)
                .Select(p => new PromotionRow(
                    p,
                    p.AssignedUser == null ? null : p.AssignedUser.FullName,
                    p.AssignedUser == null ? null : p.AssignedUser.Email
                ))
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Promotion);
        return PromotionMapper.ToDto(row);
    }

    public async Task<PromotionDto> CreateAsync(
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var code = PromotionMapper.NormalizeCode(request.Code);
        await EnsureValidAsync(0, code, request, cancellationToken);

        var entity = new Promotion { CreatedAt = DateTime.UtcNow };
        PromotionMapper.Apply(entity, code, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Promotions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.Promotion,
            entity.PromotionId.ToString(),
            null,
            AuditSnapshots.PromotionState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Promotions, cancellationToken);
        return await GetByIdAsync(entity.PromotionId, cancellationToken);
    }

    public async Task<PromotionDto> UpdateAsync(
        int id,
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var code = PromotionMapper.NormalizeCode(request.Code);
        await EnsureValidAsync(id, code, request, cancellationToken);
        await EnsureIdentityUnchangedIfRedeemedAsync(entity, code, request, cancellationToken);

        var before = AuditSnapshots.PromotionState(entity);
        PromotionMapper.Apply(entity, code, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.Promotion,
            entity.PromotionId.ToString(),
            before,
            AuditSnapshots.PromotionState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Promotions, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<PromotionDto> PauseAsync(int id, CancellationToken cancellationToken) =>
        SetActiveAsync(id, false, cancellationToken);

    public Task<PromotionDto> ResumeAsync(int id, CancellationToken cancellationToken) =>
        SetActiveAsync(id, true, cancellationToken);

    public async Task<DeletePromotionResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.PromotionState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.Promotion,
            entity.PromotionId.ToString(),
            before,
            AuditSnapshots.PromotionState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Promotions, cancellationToken);
        return new DeletePromotionResponse(id, true);
    }

    private async Task<PromotionDto> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.IsActive != isActive)
        {
            var before = AuditSnapshots.PromotionState(entity);
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            auditLogger.Record(
                AuditActions.Update,
                AuditEntityTypes.Promotion,
                entity.PromotionId.ToString(),
                before,
                AuditSnapshots.PromotionState(entity)
            );
            await db.SaveChangesAsync(cancellationToken);
            await cache.RefreshAsync(CacheKeys.Promotions, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<Promotion> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Promotion);

    private async Task EnsureIdentityUnchangedIfRedeemedAsync(
        Promotion entity,
        string code,
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var type = EnumConverters.ParseEnum<PromotionType>(request.Type, "type");
        var changed =
            entity.Code != code
            || entity.Type != type
            || entity.AssignedUserId != request.AssignedUserId;
        if (
            changed
            && await db.PromotionRedemptions.AnyAsync(
                r => r.PromotionId == entity.PromotionId,
                cancellationToken
            )
        )
            throw new ConflictAppException(Messages.Conflict_PromotionHasRedemptions);
    }

    private async Task EnsureValidAsync(
        int currentId,
        string code,
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.Promotions.AnyAsync(
            p => p.Code == code && p.PromotionId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_PromotionCodeExists);

        var errors = await PromotionReferences.FindErrorsAsync(
            db,
            request.MovieIds,
            request.ShowtimeIds,
            request.ScreenTypeIds,
            request.MembershipTierIds,
            cancellationToken
        );

        if (request.AssignedUserId is { } userId)
        {
            var isCustomer = await db.Users.AnyAsync(
                u => u.UserId == userId && u.IsActive && u.Role.RoleName == Roles.Customer,
                cancellationToken
            );
            if (!isCustomer)
                errors["assignedUserId"] = [Messages.Invalid_Promotion_AssignedUserNotFound];
        }

        if (errors.Count > 0)
            throw new ValidationAppException(Messages.Common_InvalidData, errors);
    }
}
