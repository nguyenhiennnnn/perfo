using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public static class LoyaltyRewardPromotionFactory
{
    private const string CodePrefix = "RW";
    private const int MaxCodeAttempts = 5;
    private const decimal FullPercent = 100m;

    public static Promotion Build(LoyaltyReward reward, int userId, string code, DateTime now)
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

    public static async Task<string> NewCodeAsync(
        IAppDbContext db,
        CancellationToken cancellationToken
    )
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
