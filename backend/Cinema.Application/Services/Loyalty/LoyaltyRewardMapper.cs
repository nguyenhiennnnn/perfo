using Cinema.Application.DTOs.Loyalty;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Loyalty;

public static class LoyaltyRewardMapper
{
    public static LoyaltyRewardDto ToDto(LoyaltyReward r, string? comboName) =>
        new(
            r.RewardId,
            r.Name,
            r.Type.ToString(),
            r.PointsCost,
            r.RemainingQuantity,
            r.ValidFrom,
            r.ValidTo,
            r.CodeValidityDays,
            r.DiscountType?.ToString(),
            r.DiscountValue,
            r.MaxDiscountAmount,
            r.ComboId,
            comboName,
            r.IsActive,
            r.CreatedAt,
            r.UpdatedAt
        );

    public static LoyaltyRewardCatalogItemDto ToCatalogItem(LoyaltyReward r, string? comboName) =>
        new(
            r.RewardId,
            r.Name,
            r.Type.ToString(),
            r.PointsCost,
            r.RemainingQuantity,
            r.ValidFrom,
            r.ValidTo,
            r.CodeValidityDays,
            r.DiscountType?.ToString(),
            r.DiscountValue,
            r.MaxDiscountAmount,
            r.ComboId,
            comboName
        );
}
