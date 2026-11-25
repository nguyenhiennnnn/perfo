namespace Cinema.Application.DTOs.Loyalty;

public record LoyaltyChallengeDto(
    int ChallengeId,
    string Name,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string ConditionType,
    int TargetCount,
    string RewardType,
    int? RewardPoints,
    int? RewardGiftId,
    string? RewardGiftName,
    bool IsActive,
    int ParticipantCount,
    int CompletedCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertLoyaltyChallengeRequest(
    string Name,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string ConditionType,
    int TargetCount,
    string RewardType,
    int? RewardPoints,
    int? RewardGiftId,
    bool IsActive = true
);

public record LoyaltyChallengeListFilter(string? Search, bool? IsActive);

public record DeleteLoyaltyChallengeResponse(int Id, bool Deleted);

public record MyLoyaltyChallengeDto(
    int ChallengeId,
    string Name,
    string? Description,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string ConditionType,
    int TargetCount,
    string RewardType,
    int? RewardPoints,
    string? RewardGiftName,
    int CurrentCount,
    bool IsCompleted,
    DateTime? CompletedAt,
    bool IsRewardGranted,
    bool IsRunning
);
