namespace Cinema.Application.DTOs.Orders;

public record OrderComboSuggestionDto(
    int ComboId,
    string Name,
    string? Description,
    string? ImageUrl,
    int SuggestedQuantity,
    decimal ListPrice,
    decimal PreorderPrice,
    string Label,
    bool MatchesTicketCount,
    bool PurchasedBefore
);

public record OrderComboSuggestionsDto(
    int OrderId,
    bool CanSupplement,
    int TicketCount,
    int DiscountPercent,
    DateTime? ValidUntil,
    List<OrderComboSuggestionDto> Items
);
