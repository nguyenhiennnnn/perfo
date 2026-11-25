using System.Text.Json.Serialization;

namespace Cinema.Application.DTOs.Orders;

public record OrderExchangePreviewQuery(List<int>? OrderLineIds, int? NewShowtimeId);

public record OrderExchangeTicketPreviewDto(
    int OrderLineId,
    int? TicketTypeId,
    decimal PaidAmount,
    decimal NewPrice,
    decimal Difference
);

public record OrderExchangePricingDto(
    decimal OldAmount,
    decimal NewAmount,
    decimal Difference,
    decimal AmountDue,
    decimal CreditAmount,
    IReadOnlyList<OrderExchangeTicketPreviewDto> Tickets
);

public record OrderExchangeShowtimePreviewDto(
    int ShowtimeId,
    DateTime StartTime,
    DateTime EndTime,
    int RoomId,
    string RoomName,
    bool CanExchange,
    string? Reason,
    OrderExchangePricingDto? Pricing
);

public record OrderExchangePreviewResponse(
    int OrderId,
    int MovieId,
    IReadOnlyList<OrderExchangeShowtimePreviewDto> Showtimes
);

public record ExchangeSeatRequest(int OrderLineId, int NewShowtimeSeatId, decimal? ExpectedPrice);

public record StartOrderExchangeRequest(
    int NewShowtimeId,
    List<ExchangeSeatRequest> Seats,
    bool AcceptPriceChange = false
);

public record StartOrderExchangeResponse(
    int ExchangeOrderId,
    string ExchangeOrderCode,
    int OrderId,
    string Status,
    int NewShowtimeId,
    decimal OldAmount,
    decimal NewAmount,
    decimal Difference,
    decimal AmountDue,
    decimal CreditAmount,
    DateTime ExpiresAt,
    IReadOnlyList<OrderExchangeTicketPreviewDto> Tickets,
    OrderExchangeSettlementResult? Settlement = null
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderExchangeSettlementKind
{
    None,
    ExchangeCredit,
    CashRefund,
}

public record OrderExchangeSettlementResult(
    OrderExchangeSettlementKind Kind,
    decimal Amount,
    int? GiftCardId,
    string? GiftCardCode,
    DateTime? GiftCardExpiresAt,
    IReadOnlyList<int> RefundTransactionIds
);
