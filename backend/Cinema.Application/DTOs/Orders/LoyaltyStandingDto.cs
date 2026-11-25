namespace Cinema.Application.DTOs.Orders;

public record LoyaltyStandingDto(
    int RankPoints,
    int? TierId,
    string? TierName,
    int Balance,
    int Debt
);
