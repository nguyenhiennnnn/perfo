using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyMemberQueryService(
    IAppDbContext db,
    IAppCache cache,
    ILoyaltyPointService points
) : ILoyaltyMemberQueryService
{
    public async Task<LoyaltyMemberSummaryDto> GetSummaryAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var standing = await points.GetStandingAsync(userId, cancellationToken);
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(settings.PointsExpiryReminderDays);

        var expiringLots = await db
            .LoyaltyPointEntries.AsNoTracking()
            .Where(e =>
                e.UserId == userId
                && e.RemainingPoints > 0
                && e.ExpiresAt > now
                && e.ExpiresAt <= windowEnd
            )
            .OrderBy(e => e.ExpiresAt)
            .ThenBy(e => e.EntryId)
            .Select(e => new LoyaltyExpiringLotDto(e.EntryId, e.RemainingPoints, e.ExpiresAt))
            .ToListAsync(cancellationToken);

        var nextTier = await db
            .MembershipTiers.AsNoTracking()
            .Where(t => t.MinPoints > standing.RankPoints)
            .OrderBy(t => t.MinPoints)
            .Select(t => new { t.TierName, t.MinPoints })
            .FirstOrDefaultAsync(cancellationToken);

        return new LoyaltyMemberSummaryDto(
            standing.RankPoints,
            standing.TierId,
            standing.TierName,
            nextTier?.TierName,
            nextTier is null ? null : nextTier.MinPoints - standing.RankPoints,
            standing.Balance,
            standing.Debt,
            settings.PointsExpiryReminderDays,
            expiringLots.Sum(l => l.Points),
            expiringLots
        );
    }

    public async Task<OffsetPageResult<LoyaltyLotDto>> GetLotsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.LoyaltyPointEntries.AsNoTracking().Where(e => e.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(e => e.EarnedAt)
            .ThenByDescending(e => e.EntryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var entryIds = entries.Select(e => e.EntryId).ToList();
        var usage = await db
            .LoyaltyPointTransactions.AsNoTracking()
            .Where(t =>
                t.EntryId != null
                && entryIds.Contains(t.EntryId.Value)
                && (
                    t.Type == LoyaltyTransactionType.Redeem
                    || t.Type == LoyaltyTransactionType.Expire
                )
            )
            .GroupBy(t => new { EntryId = t.EntryId!.Value, t.Type })
            .Select(g => new
            {
                g.Key.EntryId,
                g.Key.Type,
                Points = -g.Sum(t => t.Points),
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var items = entries
            .Select(e =>
            {
                var redeemed = usage
                    .Where(u => u.EntryId == e.EntryId && u.Type == LoyaltyTransactionType.Redeem)
                    .Sum(u => u.Points);
                var expired = usage
                    .Where(u => u.EntryId == e.EntryId && u.Type == LoyaltyTransactionType.Expire)
                    .Sum(u => u.Points);
                return new LoyaltyLotDto(
                    e.EntryId,
                    e.Type.ToString(),
                    e.Points,
                    redeemed,
                    expired,
                    e.RevokedPoints,
                    e.RemainingPoints,
                    e.OrderId,
                    e.EarnedAt,
                    e.ExpiresAt,
                    ResolveStatus(e.RemainingPoints, e.ExpiresAt, expired, now)
                );
            })
            .ToList();

        return OffsetPageResult<LoyaltyLotDto>.Create(items, page, pageSize, total);
    }

    private static string ResolveStatus(
        int remainingPoints,
        DateTime expiresAt,
        int expiredPoints,
        DateTime now
    )
    {
        if (expiresAt <= now && (remainingPoints > 0 || expiredPoints > 0))
            return LoyaltyLotStatus.Expired;
        return remainingPoints > 0 ? LoyaltyLotStatus.Active : LoyaltyLotStatus.Depleted;
    }
}
