namespace Cinema.Application.DTOs.Orders;

public record ComboSupplementRequest(List<ComboSelectionRequest> Combos);

public record ComboSupplementHold(int OriginalOrderId, int DiscountPercent, DateTime ValidUntilUtc);
