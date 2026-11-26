namespace Cinema.Application.DTOs.Orders;

public record ComboSelectionRequest(int ComboId, int Quantity);

public record BundleSelectionRequest(int BundleId, int Quantity);

public record SeatTicketSelectionRequest(
    int ShowtimeSeatId,
    int? TicketTypeId,
    decimal? ExpectedPrice
);

public record HoldSeatsRequest(
    int ShowtimeId,
    string CustomerName,
    string? CustomerPhone,
    List<int> SeatIds,
    List<ComboSelectionRequest>? Combos,
    List<SeatTicketSelectionRequest>? Tickets = null,
    bool AcceptPriceChange = false,
    List<BundleSelectionRequest>? Bundles = null
);

public record HoldOrderResponse(
    int OrderId,
    string OrderCode,
    string Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateTime ExpiresAt
);

public record CheckoutRequest(string PaymentMethod);

public record CheckoutResponse(string PaymentUrl, int PaymentId);
