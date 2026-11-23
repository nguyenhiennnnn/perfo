namespace Cinema.Application.DTOs.Promotions;

public record PromotionRedemptionDto(
    long PromotionRedemptionId,
    int OrderId,
    string OrderCode,
    string Channel,
    int? UserId,
    string? CustomerName,
    string Status,
    decimal DiscountAmount,
    decimal OrderFinalAmount,
    DateTime ReservedAt,
    DateTime? RedeemedAt,
    DateTime? ReleasedAt
);

public record PromotionRedemptionListFilter(
    int PromotionId,
    string? Status,
    string? Channel,
    string? Search,
    DateOnly? FromDate,
    DateOnly? ToDate
);
