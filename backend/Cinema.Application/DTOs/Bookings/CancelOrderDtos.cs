using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Bookings;

public record CancelOrderRequest(string? Reason);

public record CancelOrderRefundDto(string Method, decimal Amount, string Status);

public record CancelOrderResponse(
    int OrderId,
    string OrderCode,
    string Status,
    decimal TotalRefunded,
    string RefundStatus,
    List<CancelOrderRefundDto> Refunds
)
{
    public const string RefundStatusNone = "None";
    public const string RefundStatusRefunded = "Refunded";
    public const string RefundStatusPending = "Pending";

    public static CancelOrderResponse From(OrderCancellationResult result)
    {
        var refunds = result
            .Refunds.GroupBy(r => new { r.Method, r.Status })
            .Select(g => new CancelOrderRefundDto(
                g.Key.Method.ToDtoMethod(),
                g.Sum(r => r.Amount),
                g.Key.Status.ToDtoStatus()
            ))
            .ToList();

        var refundStatus =
            result.Refunds.Count == 0 ? RefundStatusNone
            : result.Refunds.All(r => r.Status == PaymentStatus.Success) ? RefundStatusRefunded
            : RefundStatusPending;

        return new CancelOrderResponse(
            result.OrderId,
            result.OrderCode,
            result.Status.ToDtoStatus(),
            result.TotalRefunded,
            refundStatus,
            refunds
        );
    }
}
