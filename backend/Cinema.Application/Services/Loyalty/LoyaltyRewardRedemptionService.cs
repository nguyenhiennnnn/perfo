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
    private const string CodePrefix = "RW";
    private const int MaxCodeAttempts = 5;
    private const decimal FullPercent = 100m;

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

                var promotion = BuildPromotion(reward, userId, await NewCodeAsync(cancellationToken), now);
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

    private static Promotion BuildPromotion(
        LoyaltyReward reward,
        int userId,
        string code,
        DateTime now
    )
    {
        var promotion = new Promotion
        {
            Code = code,
            Name = reward.Name,
            Type = PromotionType.PersonalCode,
            StartsAt = now,
            EndsAt = now.AddDays(reward.CodeValidityDays),
            TotalUsageLimit = 1,
            PerCustomerLimit = 1,
            AssignedUserId = userId,
            IsActive = true,
            CreatedAt = now,
        };

        switch (reward.Type)
        {
            case LoyaltyRewardType.FreeTicket:
                promotion.DiscountType = PromotionDiscountType.Percent;
                promotion.DiscountValue = FullPercent;
                promotion.MaxDiscountAmount = reward.MaxDiscountAmount;
                promotion.Scope = PromotionScope.TicketsOnly;
                break;
            case LoyaltyRewardType.FreeCombo:
                promotion.DiscountType = PromotionDiscountType.FixedAmount;
                promotion.DiscountValue = reward.Combo!.Price;
                promotion.Scope = PromotionScope.CombosOnly;
                break;
            default:
                promotion.DiscountType = reward.DiscountType!.Value;
                promotion.DiscountValue = reward.DiscountValue!.Value;
                promotion.MaxDiscountAmount = reward.MaxDiscountAmount;
                promotion.Scope = PromotionScope.WholeOrder;
                break;
        }

        return promotion;
    }

    private async Task<string> NewCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = $"{CodePrefix}-{ReferralCodeGenerator.Generate()}";
            if (!await db.Promotions.AnyAsync(p => p.Code == code, cancellationToken))
                return code;
        }

        throw new ConflictAppException(Messages.Conflict_PromotionCodeExists);
    }
}
