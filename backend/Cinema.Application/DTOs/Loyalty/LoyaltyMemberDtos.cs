namespace Cinema.Application.DTOs.Loyalty;

public static class LoyaltyLotStatus
{
    public const string Active = "Active";
    public const string Depleted = "Depleted";
    public const string Expired = "Expired";
}

public record LoyaltyExpiringLotDto(int EntryId, int Points, DateTime ExpiresAt);

public record LoyaltyMemberSummaryDto(
    int RankPoints,
    int? TierId,
    string? TierName,
    string? NextTierName,
    int? PointsToNextTier,
    int Balance,
    int Debt,
    int ExpiringWithinDays,
    int ExpiringPoints,
    List<LoyaltyExpiringLotDto> ExpiringLots
);

public record LoyaltyLotDto(
    int EntryId,
    string Type,
    int Points,
    int RedeemedPoints,
    int ExpiredPoints,
    int RevokedPoints,
    int RemainingPoints,
    int? OrderId,
    DateTime EarnedAt,
    DateTime ExpiresAt,
    string Status
);
