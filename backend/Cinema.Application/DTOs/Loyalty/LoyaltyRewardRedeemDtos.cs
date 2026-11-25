namespace Cinema.Application.DTOs.Loyalty;

public record RedeemLoyaltyRewardResponse(
    int RewardId,
    string RewardName,
    int PointsSpent,
    int BalanceAfter,
    int PromotionId,
    string Code,
    DateTime CodeExpiresAt,
    bool PointsRefundedWhenCodeExpires,
    string Notice
);
