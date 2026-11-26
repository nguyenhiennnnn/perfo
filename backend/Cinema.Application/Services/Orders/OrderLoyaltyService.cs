using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderLoyaltyService(
    IAppDbContext db,
    ILoyaltyPointService points,
    ILogger<OrderLoyaltyService> logger
) : IOrderLoyaltyService
{
    private static readonly OrderType[] EarningTypes =
    [
        OrderType.Ticket,
        OrderType.Combo,
        OrderType.Supplement,
    ];

    public async Task AwardForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new { o.UserId, o.Type })
            .FirstOrDefaultAsync(cancellationToken);
        if (order?.UserId is not { } userId || !EarningTypes.Contains(order.Type))
            return;

        var paidAmount =
            await db
                .OrderLines.AsNoTracking()
                .Where(l =>
                    l.OrderId == orderId
                    && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                )
                .SumAsync(l => (decimal?)l.PaidAmount, cancellationToken) ?? 0m;
        if (paidAmount <= 0m)
            return;

        if (
            await db.OrderLines.AnyAsync(
                l =>
                    l.OrderId == orderId
                    && l.Showtime != null
                    && l.Showtime.Kind == ShowtimeKind.Private,
                cancellationToken
            )
        )
            return;

        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            return;

        if (
            await db.LoyaltyPointEntries.AnyAsync(
                e => e.OrderId == orderId && e.Type == LoyaltyLotType.Earn,
                cancellationToken
            )
        )
            return;

        var settings = await db.GetCinemaSettingsAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var earned = LoyaltyWindow.PointsFor(paidAmount, settings.PointsPerAmount);

        if (earned > 0)
            await points.AddLotAsync(
                userId,
                LoyaltyLotType.Earn,
                earned,
                orderId,
                null,
                now,
                cancellationToken
            );

        var oldTierId = profile.TierId;
        profile.TotalSpent += paidAmount;
        await LoyaltyWindow.ResyncProfileAsync(db, profile, earned, now, cancellationToken);

        if (profile.TierId != oldTierId)
            logger.LogInformation(
                "TierChanged UserId {UserId} FromTierId {From} ToTierId {To} Points {Points}",
                userId,
                oldTierId,
                profile.TierId,
                profile.Points
            );
    }

    public async Task RevokeForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                o.UserId,
                o.Type,
                o.FinalAmount,
                o.LoyaltyReversedAmount,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (order?.UserId is not { } userId || !EarningTypes.Contains(order.Type))
            return;

        var earnedBase =
            await db
                .OrderLines.AsNoTracking()
                .Where(l =>
                    l.OrderId == orderId
                    && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                )
                .SumAsync(l => (decimal?)l.PaidAmount, cancellationToken) ?? 0m;
        var refundedTotal =
            await db
                .PaymentTransactions.AsNoTracking()
                .Where(t =>
                    t.OrderId == orderId
                    && t.Type == PaymentTransactionType.Refund
                    && (t.Status == PaymentStatus.Success || t.Status == PaymentStatus.Pending)
                )
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var targetBase = OrderRefundCalculator.LoyaltyReversalBase(
            earnedBase,
            order.FinalAmount,
            refundedTotal
        );
        var deltaBase = targetBase - order.LoyaltyReversedAmount;
        if (deltaBase <= 0m)
            return;

        var previous = order.LoyaltyReversedAmount;
        var claimed = await db
            .Orders.Where(o => o.OrderId == orderId && o.LoyaltyReversedAmount == previous)
            .ExecuteUpdateAsync(
                s => s.SetProperty(o => o.LoyaltyReversedAmount, targetBase),
                cancellationToken
            );
        if (claimed == 0)
            return;

        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            return;

        var now = DateTime.UtcNow;
        var entry = await db.LoyaltyPointEntries.FirstOrDefaultAsync(
            e => e.OrderId == orderId && e.Type == LoyaltyLotType.Earn,
            cancellationToken
        );
        if (entry is not null)
        {
            var settings = await db.GetCinemaSettingsAsync(cancellationToken);
            var targetPoints = Math.Min(
                entry.Points,
                LoyaltyWindow.PointsFor(targetBase, settings.PointsPerAmount)
            );
            var effective = targetPoints - entry.RevokedPoints;
            if (effective > 0)
            {
                entry.RevokedPoints = targetPoints;
                await points.RevokeAsync(
                    userId,
                    entry.EntryId,
                    orderId,
                    effective,
                    now,
                    cancellationToken
                );
            }
        }

        profile.TotalSpent = Math.Max(0m, profile.TotalSpent - deltaBase);
        await LoyaltyWindow.ResyncProfileAsync(db, profile, 0, now, cancellationToken);
    }
}
