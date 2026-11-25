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

public class LoyaltyAdminService(
    IAppDbContext db,
    ILoyaltyPointService points,
    IAuditLogger auditLogger
) : ILoyaltyAdminService
{
    private const int LookupLimit = 20;

    public async Task<IReadOnlyList<LoyaltyCustomerLookupItemDto>> SearchCustomersAsync(
        string? query,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var users = await db
            .Users.AsNoTracking()
            .Where(u => u.Role.RoleName == Roles.Customer && u.CustomerProfile != null)
            .ApplySearch(
                query,
                (q, textPattern, phonePattern) =>
                    q.Where(u =>
                        EF.Functions.ILike(
                            EF.Functions.Unaccent(u.FullName),
                            EF.Functions.Unaccent(textPattern)
                        )
                        || EF.Functions.ILike(u.Email, textPattern)
                        || (
                            u.Phone != null
                            && (
                                EF.Functions.ILike(u.Phone, textPattern)
                                || EF.Functions.ILike(u.Phone, phonePattern)
                            )
                        )
                    )
            )
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.UserId)
            .Take(LookupLimit)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                Debt = u.CustomerProfile!.PointDebt,
            })
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        var userIds = users.Select(u => u.UserId).ToList();
        var standings = await LoyaltyWindow.GetStandingsAsync(db, userIds, now, cancellationToken);
        var inLots = await db
            .LoyaltyPointEntries.AsNoTracking()
            .Where(e => userIds.Contains(e.UserId) && e.RemainingPoints > 0 && e.ExpiresAt > now)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(e => e.RemainingPoints) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        return users
            .Select(u =>
            {
                var standing = standings.TryGetValue(u.UserId, out var s) ? s : default;
                return new LoyaltyCustomerLookupItemDto(
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    standing.Tier?.TierId,
                    standing.Tier?.TierName,
                    standing.Points,
                    inLots.GetValueOrDefault(u.UserId) - u.Debt,
                    u.Debt
                );
            })
            .ToList();
    }

    public async Task<LoyaltyCustomerDetailDto> GetCustomerAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var user = await FindCustomerAsync(userId, cancellationToken);
        var standing = await points.GetStandingAsync(userId, cancellationToken);
        return new LoyaltyCustomerDetailDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            standing.TierId,
            standing.TierName,
            standing.RankPoints,
            standing.Balance,
            standing.Debt
        );
    }

    public async Task<OffsetPageResult<LoyaltyTransactionDto>> GetTransactionsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        await FindCustomerAsync(userId, cancellationToken);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.LoyaltyPointTransactions.AsNoTracking().Where(t => t.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.TransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var actorIds = rows
            .Where(t => t.ActorUserId is not null)
            .Select(t => t.ActorUserId!.Value)
            .Distinct()
            .ToList();
        var actorNames =
            actorIds.Count == 0
                ? new Dictionary<int, string>()
                : await db
                    .Users.AsNoTracking()
                    .Where(u => actorIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);

        return OffsetPageResult<LoyaltyTransactionDto>.Create(
            rows.Select(t => new LoyaltyTransactionDto(
                    t.TransactionId,
                    t.Type,
                    t.Points,
                    t.EntryId,
                    t.OrderId,
                    t.ActorUserId,
                    t.ActorUserId is { } actorId ? actorNames.GetValueOrDefault(actorId) : null,
                    t.CreatedAt
                ))
                .ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<LoyaltyAdjustmentResultDto> AdjustAsync(
        int userId,
        int actorUserId,
        AdjustLoyaltyPointsRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Points == 0 || Math.Abs((long)request.Points) > LoyaltyAdjustmentLimits.MaxPoints)
            throw new BusinessRuleException(Messages.Rule_LoyaltyInvalidPoints);

        await FindCustomerAsync(userId, cancellationToken);
        var reason = request.Reason.Trim();

        LoyaltyAdjustmentResultDto? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                var before = await points.GetStandingAsync(userId, cancellationToken);
                int? entryId = null;

                if (request.Points > 0)
                {
                    var lot = await points.AddLotAsync(
                        userId,
                        LoyaltyLotType.Adjustment,
                        request.Points,
                        null,
                        actorUserId,
                        DateTime.UtcNow,
                        cancellationToken
                    );
                    await db.SaveChangesAsync(cancellationToken);
                    entryId = lot.EntryId;
                }
                else
                {
                    await points.SpendAsync(
                        userId,
                        -request.Points,
                        LoyaltyTransactionType.Adjustment,
                        actorUserId,
                        cancellationToken
                    );
                }

                var after = await points.GetStandingAsync(userId, cancellationToken);
                auditLogger.Record(
                    AuditActions.AdjustLoyaltyPoints,
                    AuditEntityTypes.LoyaltyAccount,
                    userId.ToString(),
                    AuditJson.ToObject(new { balance = before.Balance, debt = before.Debt }),
                    AuditJson.ToObject(
                        new
                        {
                            balance = after.Balance,
                            debt = after.Debt,
                            adjustment = request.Points,
                            entryId,
                            reason,
                        }
                    )
                );
                await db.SaveChangesAsync(cancellationToken);

                result = new LoyaltyAdjustmentResultDto(
                    userId,
                    request.Points,
                    entryId,
                    before.Balance,
                    after.Balance,
                    before.Debt,
                    after.Debt
                );
            },
            cancellationToken
        );

        return result!;
    }

    private async Task<User> FindCustomerAsync(int userId, CancellationToken cancellationToken) =>
        await db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(
                u =>
                    u.UserId == userId
                    && u.Role.RoleName == Roles.Customer
                    && u.CustomerProfile != null,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_Customer);
}
