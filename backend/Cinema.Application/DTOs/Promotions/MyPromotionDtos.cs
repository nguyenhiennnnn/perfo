namespace Cinema.Application.DTOs.Promotions;

public record MyPromotionDto(
    int PromotionId,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    string Scope,
    decimal? MinOrderAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Channel
);
