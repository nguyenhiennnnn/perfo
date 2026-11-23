using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Orders;

internal static class OrderDetailMapper
{
    public static OrderDetailDto ToDetail(
        Order order,
        OrderPaymentSummary summary,
        IReadOnlyDictionary<int, string> tierNames,
        bool includeStaffInfo
    ) =>
        new(
            order.OrderId,
            order.OrderCode,
            order.Type.ToString(),
            order.Channel.ToDtoChannel(),
            order.Status.ToDtoStatus(),
            order.UserId,
            order.CustomerName,
            order.CustomerPhone,
            order.CreatedAt,
            order.Status == OrderStatus.Pending ? order.ExpiresAt : null,
            includeStaffInfo ? ToEmployee(order.CreatedByEmployee) : null,
            order.OriginalOrderId,
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            ToPaymentSummary(order.Status, summary),
            order.CancelReason?.ToString(),
            order.CancelledBy?.ToString(),
            order.CancelledAt,
            order
                .Lines.OrderBy(l => l.LineType)
                .ThenBy(l => l.ShowtimeSeat?.Seat.SeatRow)
                .ThenBy(l => l.ShowtimeSeat?.Seat.SeatNumber)
                .ThenBy(l => l.OrderLineId)
                .Select(l => ToLine(l, order.Status))
                .ToList(),
            order
                .PriceAdjustments.OrderBy(a => a.AdjustmentId)
                .Select(a => ToAdjustment(a, tierNames))
                .ToList(),
            order
                .PaymentTransactions.OrderBy(t => t.CreatedAt)
                .ThenBy(t => t.PaymentTransactionId)
                .Select(t => ToTransaction(t, includeStaffInfo))
                .ToList()
        );

    private static OrderEmployeeBriefDto? ToEmployee(Employee? employee) =>
        employee is null
            ? null
            : new OrderEmployeeBriefDto(employee.EmployeeId, employee.User.FullName);

    private static OrderPaymentSummaryDto ToPaymentSummary(
        OrderStatus status,
        OrderPaymentSummary summary
    ) =>
        new(
            summary.CollectedAmount,
            summary.RefundedAmount,
            summary.PendingRefundAmount,
            summary.NetPaidAmount,
            status is OrderStatus.Pending or OrderStatus.Confirmed
                ? summary.OutstandingAmount
                : 0m,
            summary.OverpaidAmount,
            summary.IsFullyPaid
        );

    private static OrderLineDto ToLine(OrderLine line, OrderStatus orderStatus) =>
        new(
            line.OrderLineId,
            line.LineType.ToString(),
            line.Description ?? line.Combo?.Name,
            line.Quantity,
            line.UnitListPrice,
            line.LockedUnitPrice,
            line.LockedUnitPrice * line.Quantity,
            line.PaidAmount,
            line.RefundedAmount,
            line.RefundedQuantity,
            line.LineType == OrderLineType.Ticket ? ToTicket(line, orderStatus) : null,
            line.LineType == OrderLineType.Combo
                ? new OrderComboDto(line.ComboId, line.Combo?.Name, line.QuantityFulfilled)
                : null
        );

    private static OrderTicketDto? ToTicket(OrderLine line, OrderStatus orderStatus)
    {
        if (line.Showtime is null || line.ShowtimeSeat is null)
            return null;

        var showtime = line.Showtime;
        var seat = line.ShowtimeSeat.Seat;
        var showQr =
            orderStatus == OrderStatus.Confirmed
            && line.RefundedQuantity < line.Quantity
            && !string.IsNullOrEmpty(line.QrCode);

        return new OrderTicketDto(
            showtime.ShowtimeId,
            showtime.Movie.MovieId,
            showtime.Movie.Title,
            showtime.Movie.PosterUrl,
            showtime.Room.RoomName,
            showtime.StartTime,
            seat.SeatRow,
            seat.SeatNumber,
            line.TicketTypeId,
            line.TicketType?.Name ?? Messages.Common_TicketTypeStandard,
            line.RequiresCardPresentation,
            line.CardCheckStatus.ToString(),
            line.CardCheckRecordedAt,
            showQr ? QrCodeHelper.GenerateDataUri(line.QrCode!) : null,
            line.IsCheckedIn,
            line.CheckedInAt
        );
    }

    private static OrderPriceAdjustmentDto ToAdjustment(
        OrderPriceAdjustment adjustment,
        IReadOnlyDictionary<int, string> tierNames
    ) =>
        new(
            adjustment.AdjustmentId,
            adjustment.Type.ToString(),
            adjustment.Amount,
            adjustment.ReferenceType,
            adjustment.ReferenceId,
            adjustment.ReferenceType == nameof(MembershipTier)
            && adjustment.ReferenceId is { } tierId
            && tierNames.TryGetValue(tierId, out var tierName)
                ? tierName
                : null
        );

    private static OrderTransactionDto ToTransaction(
        PaymentTransaction transaction,
        bool includeStaffInfo
    ) =>
        new(
            transaction.PaymentTransactionId,
            transaction.Type.ToString(),
            transaction.Method.ToDtoMethod(),
            transaction.Status.ToDtoStatus(),
            transaction.Amount,
            includeStaffInfo ? transaction.GatewayFee : null,
            transaction.TransactionCode,
            transaction.GatewayTransactionId,
            includeStaffInfo ? transaction.GatewayResponseCode : null,
            transaction.CreatedAt,
            transaction.CompletedAt,
            transaction.AmountReceived,
            transaction.ChangeGiven,
            transaction.OriginalTransactionId,
            transaction.NeedsManualRefund,
            includeStaffInfo ? transaction.Note : null
        );
}
