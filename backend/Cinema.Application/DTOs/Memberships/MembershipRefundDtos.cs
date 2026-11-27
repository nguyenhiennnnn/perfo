using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.DTOs.Memberships;

public record RefundMembershipSubscriptionRequest(string? Reason, string? Note);

public record MembershipRefundResultDto(
    int MembershipSubscriptionId,
    int OrderId,
    string OrderCode,
    bool OrderCancelled,
    decimal StoppedRevenueAmount,
    int StoppedMonths,
    int RevokedFreeTickets,
    decimal TotalRefunded,
    string RefundStatus,
    List<CancelOrderRefundDto> Refunds
);
