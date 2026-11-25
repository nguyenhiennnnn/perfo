namespace Cinema.Application.DTOs.Loyalty;

public record LoyaltyRewardDto(
    int RewardId,
    string Name,
    string Type,
    int PointsCost,
    int RemainingQuantity,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int CodeValidityDays,
    string? DiscountType,
    decimal? DiscountValue,
    decimal? MaxDiscountAmount,
    int? ComboId,
    string? ComboName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertLoyaltyRewardRequest(
    string Name,
    string Type,
    int PointsCost,
    int RemainingQuantity,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int CodeValidityDays,
    string? DiscountType,
    decimal? DiscountValue,
    decimal? MaxDiscountAmount,
    int? ComboId,
    bool IsActive = true
);

public record LoyaltyRewardListFilter(string? Search, string? Type, bool? IsActive);

public record DeleteLoyaltyRewardResponse(int Id, bool Deleted);

public record LoyaltyRewardCatalogItemDto(
    int RewardId,
    string Name,
    string Type,
    int PointsCost,
    int RemainingQuantity,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int CodeValidityDays,
    string? DiscountType,
    decimal? DiscountValue,
    decimal? MaxDiscountAmount,
    int? ComboId,
    string? ComboName
);
