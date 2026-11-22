namespace Cinema.Application.DTOs.Bookings;

public record ComboSelectionRequest(int ComboId, int Quantity);

public record HoldSeatsRequest(
    int ShowtimeId,
    string CustomerName,
    string? CustomerPhone,
    List<int> SeatIds,
    List<ComboSelectionRequest>? Combos
);

public record HoldBookingResponse(
    int BookingId,
    string BookingCode,
    string Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateTime ExpiresAt
);

public record CheckoutRequest(string PaymentMethod);

public record CheckoutResponse(string PaymentUrl, int PaymentId);
