using Cinema.Application.DTOs.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Orders;

internal record OrderListRow(
    int OrderId,
    string OrderCode,
    OrderType Type,
    SalesChannel Channel,
    OrderStatus Status,
    string? CustomerName,
    string? CustomerPhone,
    string? MovieTitle,
    DateTime? StartTime,
    int TicketCount,
    decimal FinalAmount,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int RefundCount,
    int RefundOpenCount
);

internal static class OrderListProjection
{
    public static IQueryable<OrderListRow> SelectListRows(this IQueryable<Order> query) =>
        query.Select(o => new OrderListRow(
            o.OrderId,
            o.OrderCode,
            o.Type,
            o.Channel,
            o.Status,
            o.CustomerName,
            o.CustomerPhone,
            o.Lines.Where(l => l.LineType == OrderLineType.Ticket && l.Showtime != null)
                .OrderBy(l => l.OrderLineId)
                .Select(l => l.Showtime!.Movie.Title)
                .FirstOrDefault(),
            o.Lines.Where(l => l.LineType == OrderLineType.Ticket && l.Showtime != null)
                .OrderBy(l => l.OrderLineId)
                .Select(l => (DateTime?)l.Showtime!.StartTime)
                .FirstOrDefault(),
            o.Lines.Where(l => l.LineType == OrderLineType.Ticket)
                .Sum(l => (int?)l.Quantity) ?? 0,
            o.FinalAmount,
            o.CreatedAt,
            o.ExpiresAt,
            o.PaymentTransactions.Count(t => t.Type == PaymentTransactionType.Refund),
            o.PaymentTransactions.Count(t =>
                t.Type == PaymentTransactionType.Refund && t.Status != PaymentStatus.Success
            )
        ));

    public static OrderListItemDto ToDto(this OrderListRow row) =>
        new(
            row.OrderId,
            row.OrderCode,
            row.Type.ToString(),
            row.Channel.ToDtoChannel(),
            row.Status.ToDtoStatus(),
            row.CustomerName,
            row.CustomerPhone,
            row.MovieTitle,
            row.StartTime,
            row.TicketCount,
            row.FinalAmount,
            row.CreatedAt,
            row.Status == OrderStatus.Pending ? row.ExpiresAt : null,
            row.RefundCount == 0 ? CancelOrderResponse.RefundStatusNone
            : row.RefundOpenCount == 0 ? CancelOrderResponse.RefundStatusRefunded
            : CancelOrderResponse.RefundStatusPending
        );
}
