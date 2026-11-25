namespace Cinema.Application.DTOs.Loyalty;

public record ReferralItemDto(
    string ReferredName,
    DateTime ReferredAt,
    bool RewardGranted,
    DateTime? RewardGrantedAt
);

public record MyReferralsDto(
    string? ReferralCode,
    int TotalCount,
    int RewardedCount,
    IReadOnlyList<ReferralItemDto> Items
);
