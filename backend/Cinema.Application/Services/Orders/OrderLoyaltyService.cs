using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderLoyaltyService(
    IAppDbContext db,
    ILoyaltyPointService points,
    ILogger<OrderLoyaltyService> logger
)
    : IOrderLoyaltyService
{
    public async Task AwardAsync(
        int userId,
        int orderId,
        decimal paidAmount,
        CancellationToken cancellationToken
    )
    {
        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            return;

        if (await db.LoyaltyPointEntries.AnyAsync(e => e.OrderId == orderId, cancellationToken))
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

    public async Task ReverseAsync(
        int userId,
        int orderId,
        decimal refundedAmount,
        CancellationToken cancellationToken
    )
    {
        var settings = await db.GetCinemaSettingsAsync(cancellationToken);
        await ReverseAsync(userId, orderId, refundedAmount, settings, cancellationToken);
    }

    public async Task ReverseAsync(
        int userId,
        int orderId,
        decimal refundedAmount,
        CinemaSettings settings,
        CancellationToken cancellationToken
    )
    {
        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            return;

        var entry = await db.LoyaltyPointEntries.FirstOrDefaultAsync(
            e => e.OrderId == orderId,
            cancellationToken
        );
        if (entry is not null)
        {
            var revoke = LoyaltyWindow.PointsFor(refundedAmount, settings.PointsPerAmount);
            var revokedBefore = entry.RevokedPoints;
            entry.RevokedPoints = Math.Min(entry.Points, entry.RevokedPoints + revoke);
            var effective = entry.RevokedPoints - revokedBefore;
            if (effective > 0)
                await points.RevokeAsync(
                    userId,
                    entry.EntryId,
                    orderId,
                    effective,
                    DateTime.UtcNow,
                    cancellationToken
                );
        }

        profile.TotalSpent = Math.Max(0, profile.TotalSpent - refundedAmount);
        await LoyaltyWindow.ResyncProfileAsync(
            db,
            profile,
            pendingNewPoints: 0,
            DateTime.UtcNow,
            cancellationToken
        );
    }
}
