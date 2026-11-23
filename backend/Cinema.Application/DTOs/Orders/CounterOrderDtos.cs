using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Orders;

public record CounterHoldRequest(
    int ShowtimeId,
    List<int> SeatIds,
    List<ComboSelectionRequest>? Combos,
    int? UserId,
    string CustomerName,
    string? CustomerPhone,
    List<SeatTicketSelectionRequest>? Tickets = null,
    bool AcceptPriceChange = false
);

public record CounterComboOrderRequest(
    List<ComboSelectionRequest> Combos,
    int? UserId,
    string CustomerName,
    string? CustomerPhone
);

public record CounterOrderResponse(
    int OrderId,
    string OrderCode,
    string Type,
    string Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateTime ExpiresAt,
    string Channel,
    int? CreatedByEmployeeId,
    int? UserId,
    string? CustomerName,
    string? CustomerPhone
)
{
    public static CounterOrderResponse From(Order order) =>
        new(
            order.OrderId,
            order.OrderCode,
            order.Type.ToString(),
            order.Status.ToString(),
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.ExpiresAt!.Value,
            order.Channel == SalesChannel.Counter ? "Counter" : "Online",
            order.CreatedByEmployeeId,
            order.UserId,
            order.CustomerName,
            order.CustomerPhone
        );
}

public record CounterCheckoutRequest(
    string PaymentMethod,
    decimal? Amount,
    decimal? AmountReceived
);

public record CounterCheckoutResponse(
    int OrderId,
    string OrderCode,
    string Status,
    int PaymentTransactionId,
    string TransactionCode,
    string PaymentMethod,
    decimal AmountPaid,
    decimal? AmountReceived,
    decimal Change,
    decimal TotalPaid,
    decimal OutstandingAmount,
    bool IsFullyPaid
);

public record CheckinRequest(string QrCode);

public record CheckinResponse(
    int OrderLineId,
    int OrderId,
    string OrderCode,
    string SeatRow,
    int SeatNumber,
    string MovieTitle,
    string RoomName,
    DateTime StartTime,
    DateTime CheckedInAt,
    int? TicketTypeId,
    string TicketTypeName,
    bool RequiresCardPresentation,
    string CardCheckStatus
);

public record RecordCardNotPresentedRequest(int OrderLineId);

public record RecordCardNotPresentedResponse(
    int OrderLineId,
    int OrderId,
    string CardCheckStatus,
    DateTime CardCheckRecordedAt
);

public record CounterSupplementOrderRequest(int OrderLineId, int? ReplacementTicketTypeId);
