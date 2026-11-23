using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Pricing;

public class TicketTypeService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : ITicketTypeService
{
    public async Task<OffsetPageResult<TicketTypeDto>> GetAdminAsync(
        int page,
        int pageSize,
        TicketTypeListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.TicketTypes.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(t => t.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(t => EF.Functions.ILike(t.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.PriceMultiplier)
            .ThenBy(t => t.TicketTypeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<TicketTypeDto>.Create(
            items.Select(PricingMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<TicketTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        PricingMapper.ToDto(
            await db
                .TicketTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TicketTypeId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_TicketType)
        );

    public async Task<TicketTypeDto> CreateAsync(
        UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    )
    {
        var name = request.Name.Trim();
        await EnsureValidAsync(0, name, request, cancellationToken);

        var entity = new TicketType { CreatedAt = DateTime.UtcNow };
        Apply(entity, name, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.TicketTypes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.TicketType,
            entity.TicketTypeId.ToString(),
            null,
            AuditSnapshots.TicketTypeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.TicketTypes, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<TicketTypeDto> UpdateAsync(
        int id,
        UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureValidAsync(id, name, request, cancellationToken);

        var before = AuditSnapshots.TicketTypeState(entity);
        Apply(entity, name, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.TicketType,
            entity.TicketTypeId.ToString(),
            before,
            AuditSnapshots.TicketTypeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.TicketTypes, cancellationToken);
        return PricingMapper.ToDto(entity);
    }

    public async Task<DeletePricingEntityResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.TicketTypeState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.TicketType,
            entity.TicketTypeId.ToString(),
            before,
            AuditSnapshots.TicketTypeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.TicketTypes, cancellationToken);
        return new DeletePricingEntityResponse(id, true);
    }

    private async Task<TicketType> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.TicketTypes.FirstOrDefaultAsync(t => t.TicketTypeId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_TicketType);

    private async Task EnsureValidAsync(
        int currentId,
        string name,
        UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.TicketTypes.AnyAsync(
            t => t.Name == name && t.TicketTypeId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_TicketTypeExists);

        var ageRatingIds = PricingMapper.NormalizeIds(request.AllowedAgeRatingIds);
        if (ageRatingIds.Count == 0)
            return;

        var found = await db.AgeRatings.CountAsync(
            a => ageRatingIds.Contains(a.AgeRatingId),
            cancellationToken
        );
        if (found != ageRatingIds.Count)
            throw new ValidationAppException(
                Messages.Invalid_TicketType_AgeRatingNotFound,
                new Dictionary<string, string[]>
                {
                    ["allowedAgeRatingIds"] = [Messages.Invalid_TicketType_AgeRatingNotFound],
                }
            );
    }

    private static void Apply(TicketType entity, string name, UpsertTicketTypeRequest request)
    {
        entity.Name = name;
        entity.PriceMultiplier = request.PriceMultiplier;
        entity.RequiresCardVerification = request.RequiresCardVerification;
        entity.AllowedAgeRatingIds = PricingMapper.NormalizeIds(request.AllowedAgeRatingIds);
        entity.AllowedFromTime = DateTimeUtils.ParseTimeOnly(request.AllowedFromTime);
        entity.AllowedToTime = DateTimeUtils.ParseTimeOnly(request.AllowedToTime);
        entity.AllowedDaysOfWeek = PricingMapper.NormalizeIds(request.AllowedDaysOfWeek);
        entity.IsActive = request.IsActive;
    }
}
