using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyPointService(IAppDbContext db) : ILoyaltyPointService
{
    private const int MaxTakeAttempts = 5;
    private const int ExpireBatchSize = 500;

    public async Task<int> GetBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var inLots =
            await db
                .LoyaltyPointEntries.AsNoTracking()
                .Where(e => e.UserId == userId && e.RemainingPoints > 0 && e.ExpiresAt > now)
                .SumAsync(e => (int?)e.RemainingPoints, cancellationToken) ?? 0;
        return inLots - await GetDebtAsync(userId, cancellationToken);
    }

    public async Task<LoyaltyStandingDto> GetStandingAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var (rankPoints, tier) = await LoyaltyWindow.GetStandingAsync(
            db,
            userId,
            DateTime.UtcNow,
            cancellationToken
        );
        var debt = await GetDebtAsync(userId, cancellationToken);
        var balance = await GetBalanceAsync(userId, cancellationToken);
        return new LoyaltyStandingDto(rankPoints, tier?.TierId, tier?.TierName, balance, debt);
    }

    public async Task<LoyaltyPointEntry> AddLotAsync(
        int userId,
        LoyaltyLotType type,
        int points,
        int? orderId,
        int? actorUserId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (points <= 0)
            throw new BusinessRuleException(Messages.Rule_LoyaltyInvalidPoints);

        var months =
            await db
                .CinemaSettings.Where(s => s.Id == AppDbContextExtensions.CinemaSettingsId)
                .Select(s => (int?)s.PointsExpiryMonths)
                .FirstOrDefaultAsync(cancellationToken)
            ?? CinemaSettings.DefaultPointsExpiryMonths;

        var offset = 0;
        var debt = await GetDebtAsync(userId, cancellationToken);
        if (debt > 0)
        {
            var candidate = Math.Min(debt, points);
            var settled =
                await db
                    .CustomerProfiles.Where(p => p.UserId == userId && p.PointDebt >= candidate)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.PointDebt, p => p.PointDebt - candidate),
                        cancellationToken
                    ) == 1;
            if (settled)
                offset = candidate;
        }

        var entry = new LoyaltyPointEntry
        {
            UserId = userId,
            OrderId = orderId,
            Type = type,
            Points = points,
            RemainingPoints = points - offset,
            EarnedAt = now,
            ExpiresAt = now.AddMonths(months),
        };
        db.LoyaltyPointEntries.Add(entry);

        db.LoyaltyPointTransactions.Add(
            new LoyaltyPointTransaction
            {
                UserId = userId,
                Entry = entry,
                OrderId = orderId,
                Type = ToTransactionType(type),
                Points = points,
                ActorUserId = actorUserId,
                CreatedAt = now,
            }
        );
        if (offset > 0)
            db.LoyaltyPointTransactions.Add(
                new LoyaltyPointTransaction
                {
                    UserId = userId,
                    Entry = entry,
                    OrderId = orderId,
                    Type = LoyaltyTransactionType.Revoke,
                    Points = -offset,
                    CreatedAt = now,
                }
            );

        return entry;
    }

    public async Task SpendAsync(
        int userId,
        int points,
        LoyaltyTransactionType type,
        int? actorUserId,
        CancellationToken cancellationToken
    )
    {
        if (points <= 0 || type is not (LoyaltyTransactionType.Redeem or LoyaltyTransactionType.Adjustment))
            throw new BusinessRuleException(Messages.Rule_LoyaltyInvalidPoints);

        await db.InTransactionAsync(
            async () =>
            {
                var now = DateTime.UtcNow;
                if (await GetBalanceAsync(userId, cancellationToken) < points)
                    throw new BusinessRuleException(Messages.Rule_LoyaltyInsufficientPoints);

                var lots = await db
                    .LoyaltyPointEntries.AsNoTracking()
                    .Where(e => e.UserId == userId && e.RemainingPoints > 0 && e.ExpiresAt > now)
                    .OrderBy(e => e.EarnedAt)
                    .ThenBy(e => e.EntryId)
                    .Select(e => new { e.EntryId, e.OrderId })
                    .ToListAsync(cancellationToken);

                var needed = points;
                foreach (var lot in lots)
                {
                    if (needed == 0)
                        break;
                    var taken = await TakeUpToAsync(lot.EntryId, needed, cancellationToken);
                    if (taken == 0)
                        continue;

                    needed -= taken;
                    db.LoyaltyPointTransactions.Add(
                        new LoyaltyPointTransaction
                        {
                            UserId = userId,
                            EntryId = lot.EntryId,
                            Type = type,
                            Points = -taken,
                            ActorUserId = actorUserId,
                            CreatedAt = now,
                        }
                    );
                }

                if (needed > 0)
                    throw new BusinessRuleException(Messages.Rule_LoyaltyInsufficientPoints);

                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    public async Task RevokeAsync(
        int userId,
        int entryId,
        int orderId,
        int points,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (points <= 0)
            return;

        var taken = await TakeUpToAsync(entryId, points, cancellationToken);
        var uncovered = points - taken;
        if (uncovered > 0)
            await db
                .CustomerProfiles.Where(p => p.UserId == userId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.PointDebt, p => p.PointDebt + uncovered),
                    cancellationToken
                );

        db.LoyaltyPointTransactions.Add(
            new LoyaltyPointTransaction
            {
                UserId = userId,
                EntryId = entryId,
                OrderId = orderId,
                Type = LoyaltyTransactionType.Revoke,
                Points = -points,
                CreatedAt = now,
            }
        );
    }

    public async Task<int> ExpireAsync(DateTime now, CancellationToken cancellationToken)
    {
        var expired = 0;
        while (true)
        {
            var lots = await db
                .LoyaltyPointEntries.AsNoTracking()
                .Where(e => e.RemainingPoints > 0 && e.ExpiresAt <= now)
                .OrderBy(e => e.EntryId)
                .Take(ExpireBatchSize)
                .Select(e => new { e.EntryId, e.UserId })
                .ToListAsync(cancellationToken);
            if (lots.Count == 0)
                break;

            var batchExpired = 0;
            await db.InTransactionAsync(
                async () =>
                {
                    batchExpired = 0;
                    foreach (var lot in lots)
                    {
                        var taken = await TakeUpToAsync(lot.EntryId, int.MaxValue, cancellationToken);
                        if (taken == 0)
                            continue;

                        batchExpired++;
                        db.LoyaltyPointTransactions.Add(
                            new LoyaltyPointTransaction
                            {
                                UserId = lot.UserId,
                                EntryId = lot.EntryId,
                                Type = LoyaltyTransactionType.Expire,
                                Points = -taken,
                                CreatedAt = now,
                            }
                        );
                    }

                    await db.SaveChangesAsync(cancellationToken);
                },
                cancellationToken
            );

            expired += batchExpired;
            if (batchExpired == 0)
                break;
        }

        return expired;
    }

    private async Task<int> TakeUpToAsync(int entryId, int wanted, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxTakeAttempts; attempt++)
        {
            var remaining = await db
                .LoyaltyPointEntries.AsNoTracking()
                .Where(e => e.EntryId == entryId)
                .Select(e => e.RemainingPoints)
                .FirstOrDefaultAsync(cancellationToken);
            var take = Math.Min(remaining, wanted);
            if (take <= 0)
                return 0;

            var updated = await db
                .LoyaltyPointEntries.Where(e => e.EntryId == entryId && e.RemainingPoints >= take)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.RemainingPoints, e => e.RemainingPoints - take),
                    cancellationToken
                );
            if (updated == 1)
                return take;
        }

        return 0;
    }

    private async Task<int> GetDebtAsync(int userId, CancellationToken cancellationToken) =>
        await db
            .CustomerProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.PointDebt)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

    private static LoyaltyTransactionType ToTransactionType(LoyaltyLotType type) =>
        type switch
        {
            LoyaltyLotType.Earn => LoyaltyTransactionType.Earn,
            LoyaltyLotType.Bonus => LoyaltyTransactionType.Bonus,
            _ => LoyaltyTransactionType.Adjustment,
        };
}
