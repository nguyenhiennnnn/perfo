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

public class LoyaltyChallengeAdminService(IAppDbContext db, IAuditLogger auditLogger)
    : ILoyaltyChallengeAdminService
{
    public async Task<OffsetPageResult<LoyaltyChallengeDto>> GetAsync(
        int page,
        int pageSize,
        LoyaltyChallengeListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.LoyaltyChallenges.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(c => c.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(c => EF.Functions.ILike(c.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(c => c.StartsOn)
            .ThenByDescending(c => c.ChallengeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new Row(
                c,
                c.RewardGift != null ? c.RewardGift.Name : null,
                c.Progresses.Count,
                c.Progresses.Count(p => p.CompletedAt != null)
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<LoyaltyChallengeDto>.Create(
            rows.Select(ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<LoyaltyChallengeDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    ) =>
        ToDto(
            await db
                .LoyaltyChallenges.AsNoTracking()
                .Where(c => c.ChallengeId == id)
                .Select(c => new Row(
                    c,
                    c.RewardGift != null ? c.RewardGift.Name : null,
                    c.Progresses.Count,
                    c.Progresses.Count(p => p.CompletedAt != null)
                ))
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_LoyaltyChallenge)
        );

    public async Task<LoyaltyChallengeDto> CreateAsync(
        UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    )
    {
        var giftName = await ResolveGiftNameAsync(request, cancellationToken);
        var entity = new LoyaltyChallenge { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.LoyaltyChallenges.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.LoyaltyChallenge,
            entity.ChallengeId.ToString(),
            null,
            AuditSnapshots.LoyaltyChallengeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDto(new Row(entity, giftName, 0, 0));
    }

    public async Task<LoyaltyChallengeDto> UpdateAsync(
        int id,
        UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var giftName = await ResolveGiftNameAsync(request, cancellationToken);

        var conditionType = Enum.Parse<LoyaltyChallengeConditionType>(
            request.ConditionType.Trim(),
            true
        );
        if (
            conditionType != entity.ConditionType
            && await db.LoyaltyChallengeProgresses.AnyAsync(
                p => p.ChallengeId == id,
                cancellationToken
            )
        )
            throw new ConflictAppException(Messages.Conflict_LoyaltyChallengeHasProgress);

        var before = AuditSnapshots.LoyaltyChallengeState(entity);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.LoyaltyChallenge,
            entity.ChallengeId.ToString(),
            before,
            AuditSnapshots.LoyaltyChallengeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<DeleteLoyaltyChallengeResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.LoyaltyChallengeState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.LoyaltyChallenge,
            entity.ChallengeId.ToString(),
            before,
            AuditSnapshots.LoyaltyChallengeState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteLoyaltyChallengeResponse(id, true);
    }

    private async Task<LoyaltyChallenge> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.LoyaltyChallenges.FirstOrDefaultAsync(c => c.ChallengeId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_LoyaltyChallenge);

    private async Task<string?> ResolveGiftNameAsync(
        UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.RewardGiftId is not { } giftId)
            return null;

        return await db
                .LoyaltyRewards.AsNoTracking()
                .Where(r => r.RewardId == giftId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_LoyaltyReward);
    }

    private static void Apply(LoyaltyChallenge entity, UpsertLoyaltyChallengeRequest request)
    {
        var rewardType = Enum.Parse<LoyaltyChallengeRewardType>(request.RewardType.Trim(), true);

        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.StartsOn = request.StartsOn;
        entity.EndsOn = request.EndsOn;
        entity.ConditionType = Enum.Parse<LoyaltyChallengeConditionType>(
            request.ConditionType.Trim(),
            true
        );
        entity.TargetCount = request.TargetCount;
        entity.RewardType = rewardType;
        entity.RewardPoints = rewardType == LoyaltyChallengeRewardType.Points ? request.RewardPoints : null;
        entity.RewardGiftId = rewardType == LoyaltyChallengeRewardType.Gift ? request.RewardGiftId : null;
        entity.IsActive = request.IsActive;
    }

    private static LoyaltyChallengeDto ToDto(Row row) =>
        new(
            row.Challenge.ChallengeId,
            row.Challenge.Name,
            row.Challenge.Description,
            row.Challenge.StartsOn,
            row.Challenge.EndsOn,
            row.Challenge.ConditionType.ToString(),
            row.Challenge.TargetCount,
            row.Challenge.RewardType.ToString(),
            row.Challenge.RewardPoints,
            row.Challenge.RewardGiftId,
            row.GiftName,
            row.Challenge.IsActive,
            row.Participants,
            row.Completed,
            row.Challenge.CreatedAt,
            row.Challenge.UpdatedAt
        );

    private sealed record Row(
        LoyaltyChallenge Challenge,
        string? GiftName,
        int Participants,
        int Completed
    );
}
