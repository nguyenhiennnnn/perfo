using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyRewardRedemptionService(IAppDbContext db, ILoyaltyPointService points)
    : ILoyaltyRewardRedemptionService
{
    public async Task<RedeemLoyaltyRewardResponse> RedeemAsync(
        int userId,
        int rewardId,
        CancellationToken cancellationToken
    )
    {
        var isCustomer = await db
            .Users.AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserId == userId
                    && u.IsActive
                    && u.Role.RoleName == Roles.Customer
                    && u.CustomerProfile != null,
                cancellationToken
            );
        if (!isCustomer)
            throw new NotFoundException(Messages.NotFound_Customer);

        RedeemLoyaltyRewardResponse? response = null;
        await db.InTransactionAsync(
            async () =>
            {
                var now = DateTime.UtcNow;
                var reward = await db
                    .LoyaltyRewards.AsNoTracking()
                    .Include(r => r.Combo)
                    .FirstOrDefaultAsync(r => r.RewardId == rewardId, cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_LoyaltyReward);
                if (!IsRedeemable(reward, now))
                    throw new BusinessRuleException(Messages.Rule_LoyaltyRewardUnavailable);

                await points.SpendAsync(
                    userId,
                    reward.PointsCost,
                    LoyaltyTransactionType.Redeem,
                    userId,
                    cancellationToken
                );

                var reserved = await db
                    .LoyaltyRewards.Where(r =>
                        r.RewardId == rewardId
                        && r.IsActive
                        && r.RemainingQuantity > 0
                        && (r.ValidFrom == null || r.ValidFrom <= now)
                        && (r.ValidTo == null || r.ValidTo > now)
                    )
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(r => r.RemainingQuantity, r => r.RemainingQuantity - 1)
                                .SetProperty(r => r.UpdatedAt, now),
                        cancellationToken
                    );
                if (reserved != 1)
                    throw new BusinessRuleException(Messages.Rule_LoyaltyRewardUnavailable);

                var promotion = LoyaltyRewardPromotionFactory.Build(
                    reward,
                    userId,
                    await LoyaltyRewardPromotionFactory.NewCodeAsync(db, cancellationToken),
                    now
                );
                db.Promotions.Add(promotion);
                await db.SaveChangesAsync(cancellationToken);

                response = new RedeemLoyaltyRewardResponse(
                    reward.RewardId,
                    reward.Name,
                    reward.PointsCost,
                    await points.GetBalanceAsync(userId, cancellationToken),
                    promotion.PromotionId,
                    promotion.Code,
                    promotion.EndsAt!.Value,
                    false,
                    Messages.Info_LoyaltyRewardCodeNoRefund
                );
            },
            cancellationToken
        );

        return response!;
    }

    private static bool IsRedeemable(LoyaltyReward reward, DateTime now) =>
        reward.IsActive
        && reward.RemainingQuantity > 0
        && (reward.ValidFrom is null || reward.ValidFrom <= now)
        && (reward.ValidTo is null || reward.ValidTo > now)
        && (reward.Combo is null || reward.Combo.IsActive);
}
