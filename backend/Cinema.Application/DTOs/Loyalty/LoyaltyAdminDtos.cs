using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Loyalty;

public record LoyaltyCustomerLookupItemDto(
    int UserId,
    string FullName,
    string Email,
    string? Phone,
    int? TierId,
    string? TierName,
    int RankPoints,
    int Balance,
    int Debt
);

public record LoyaltyCustomerDetailDto(
    int UserId,
    string FullName,
    string Email,
    string? Phone,
    int? TierId,
    string? TierName,
    int RankPoints,
    int Balance,
    int Debt
);

public record LoyaltyTransactionDto(
    long TransactionId,
    LoyaltyTransactionType Type,
    int Points,
    int? EntryId,
    int? OrderId,
    int? ActorUserId,
    string? ActorName,
    DateTime CreatedAt
);

public record AdjustLoyaltyPointsRequest(int Points, string Reason);

public record LoyaltyAdjustmentResultDto(
    int UserId,
    int Points,
    int? EntryId,
    int BalanceBefore,
    int BalanceAfter,
    int DebtBefore,
    int DebtAfter
);
