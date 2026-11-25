using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyRewardAdminService(IAppDbContext db, IAuditLogger auditLogger)
    : ILoyaltyRewardAdminService
{
    public async Task<OffsetPageResult<LoyaltyRewardDto>> GetAsync(
        int page,
        int pageSize,
        LoyaltyRewardListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.LoyaltyRewards.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(r => r.IsActive == isActive);
        if (
            !string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<LoyaltyRewardType>(filter.Type.Trim(), true, out var type)
        )
            query = query.Where(r => r.Type == type);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(r => EF.Functions.ILike(r.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.PointsCost)
            .ThenBy(r => r.RewardId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Reward = r, ComboName = r.Combo != null ? r.Combo.Name : null })
            .ToListAsync(cancellationToken);

        return OffsetPageResult<LoyaltyRewardDto>.Create(
            items.Select(i => LoyaltyRewardMapper.ToDto(i.Reward, i.ComboName)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<LoyaltyRewardDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var item =
            await db
                .LoyaltyRewards.AsNoTracking()
                .Where(r => r.RewardId == id)
                .Select(r => new { Reward = r, ComboName = r.Combo != null ? r.Combo.Name : null })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_LoyaltyReward);
        return LoyaltyRewardMapper.ToDto(item.Reward, item.ComboName);
    }

    public async Task<LoyaltyRewardDto> CreateAsync(
        UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    )
    {
        var comboName = await ResolveComboNameAsync(request, cancellationToken);
        var entity = new LoyaltyReward { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.LoyaltyRewards.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.LoyaltyReward,
            entity.RewardId.ToString(),
            null,
            AuditSnapshots.LoyaltyRewardState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return LoyaltyRewardMapper.ToDto(entity, comboName);
    }

    public async Task<LoyaltyRewardDto> UpdateAsync(
        int id,
        UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var comboName = await ResolveComboNameAsync(request, cancellationToken);

        var before = AuditSnapshots.LoyaltyRewardState(entity);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.LoyaltyReward,
            entity.RewardId.ToString(),
            before,
            AuditSnapshots.LoyaltyRewardState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return LoyaltyRewardMapper.ToDto(entity, comboName);
    }

    public async Task<DeleteLoyaltyRewardResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.LoyaltyRewardState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.LoyaltyReward,
            entity.RewardId.ToString(),
            before,
            AuditSnapshots.LoyaltyRewardState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteLoyaltyRewardResponse(id, true);
    }

    private async Task<LoyaltyReward> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.LoyaltyRewards.FirstOrDefaultAsync(r => r.RewardId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_LoyaltyReward);

    private async Task<string?> ResolveComboNameAsync(
        UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.ComboId is not { } comboId)
            return null;

        return await db
                .Combos.AsNoTracking()
                .Where(c => c.ComboId == comboId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Combo);
    }

    private static void Apply(LoyaltyReward entity, UpsertLoyaltyRewardRequest request)
    {
        var type = Enum.Parse<LoyaltyRewardType>(request.Type.Trim(), true);
        var isDiscount = type == LoyaltyRewardType.Discount;

        entity.Name = request.Name.Trim();
        entity.Type = type;
        entity.PointsCost = request.PointsCost;
        entity.RemainingQuantity = request.RemainingQuantity;
        entity.ValidFrom = request.ValidFrom is { } from ? BusinessTime.ToUtc(from) : null;
        entity.ValidTo = request.ValidTo is { } to ? BusinessTime.ToUtc(to) : null;
        entity.CodeValidityDays = request.CodeValidityDays;
        entity.DiscountType = isDiscount
            ? Enum.Parse<PromotionDiscountType>(request.DiscountType!.Trim(), true)
            : null;
        entity.DiscountValue = isDiscount ? request.DiscountValue : null;
        entity.MaxDiscountAmount =
            type == LoyaltyRewardType.FreeTicket
            || (isDiscount && entity.DiscountType == PromotionDiscountType.Percent)
                ? request.MaxDiscountAmount
                : null;
        entity.ComboId = type == LoyaltyRewardType.FreeCombo ? request.ComboId : null;
        entity.IsActive = request.IsActive;
    }
}
