namespace Cinema.Application.DTOs.Orders;

public record ApplyPromotionRequest(string Code);

public record OrderPromotionResponse(
    int OrderId,
    string OrderCode,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateTime ExpiresAt,
    string? PromotionCode,
    string? PromotionName,
    decimal PromotionDiscount
);
