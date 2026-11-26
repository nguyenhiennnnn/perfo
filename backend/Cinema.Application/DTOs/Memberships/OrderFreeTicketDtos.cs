namespace Cinema.Application.DTOs.Memberships;

public record ApplyFreeTicketRequest(int OrderLineId);

public record OrderFreeTicketItemDto(
    int OrderLineId,
    int MembershipFreeTicketId,
    decimal ConvertedAmount
);

public record OrderFreeTicketResponse(
    int OrderId,
    string OrderCode,
    string Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    decimal FreeTicketAmount,
    decimal PayableAmount,
    bool IsFullyCovered,
    int RemainingFreeTickets,
    IReadOnlyList<OrderFreeTicketItemDto> Tickets
);
