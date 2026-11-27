using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public static class CampaignPromotionFactory
{
    private const string CodePrefix = "CD";

    public static Promotion Build(Campaign campaign, int userId, string code, DateTime now) =>
        new()
        {
            Code = code,
            Name = campaign.Name,
            Type = PromotionType.PersonalCode,
            DiscountType = campaign.PromoDiscountType!.Value,
            DiscountValue = campaign.PromoDiscountValue!.Value,
            MaxDiscountAmount =
                campaign.PromoDiscountType == PromotionDiscountType.Percent
                    ? campaign.PromoMaxDiscountAmount
                    : null,
            Scope = PromotionScope.WholeOrder,
            StartsAt = now,
            EndsAt = now.AddDays(campaign.PromoValidityDays!.Value),
            TotalUsageLimit = 1,
            PerCustomerLimit = 1,
            AssignedUserId = userId,
            IsActive = true,
            CreatedAt = now,
        };

    public static async Task<string> NewCodeAsync(
        IAppDbContext db,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 0; attempt < PromotionLimits.GenerateCodeMaxAttempts; attempt++)
        {
            var code = $"{CodePrefix}-{ReferralCodeGenerator.Generate()}";
            if (!await db.Promotions.AnyAsync(p => p.Code == code, cancellationToken))
                return code;
        }

        throw new ConflictAppException(Messages.Conflict_PromotionCodeExists);
    }
}
