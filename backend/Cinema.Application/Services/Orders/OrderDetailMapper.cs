using Cinema.Application.DTOs.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Cinema.Application.Interfaces.Ledger;


namespace Cinema.Application.Services.Orders;

internal sealed record OrderPromotionInfo(string Code, string Name, bool IsCodeBased);

internal static class OrderDetailMapper
{
    public static OrderDetailDto ToDetail(
        Order order,
        OrderPaymentSummary summary,
        IReadOnlyDictionary<int, string> tierNames,
        IReadOnlyDictionary<int, OrderPromotionInfo> promotions,
        IReadOnlyDictionary<int, string> bundleNames,
        bool includeStaffInfo,
        List<OrderExchangeHistoryDto>? exchanges = null
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
                .Select(a => ToAdjustment(a, tierNames, promotions, bundleNames))
                .ToList(),
            order
                .PaymentTransactions.OrderBy(t => t.CreatedAt)
                .ThenBy(t => t.PaymentTransactionId)
                .Select(t => ToTransaction(t, includeStaffInfo))
                .ToList(),
            exchanges ?? []
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
        IReadOnlyDictionary<int, string> tierNames,
        IReadOnlyDictionary<int, OrderPromotionInfo> promotions,
        IReadOnlyDictionary<int, string> bundleNames
    )
    {
        var promotion =
            adjustment.ReferenceType == nameof(Promotion)
            && adjustment.ReferenceId is { } promotionId
            && promotions.TryGetValue(promotionId, out var info)
                ? info
                : null;

        var referenceName =
            adjustment.ReferenceType == nameof(MembershipTier)
            && adjustment.ReferenceId is { } tierId
            && tierNames.TryGetValue(tierId, out var tierName)
                ? tierName
                : adjustment.ReferenceType == nameof(Bundle)
                && adjustment.ReferenceId is { } bundleId
                && bundleNames.TryGetValue(bundleId, out var bundleName)
                    ? bundleName
                    : promotion?.Name;

        return new OrderPriceAdjustmentDto(
            adjustment.AdjustmentId,
            adjustment.Type.ToString(),
            adjustment.Amount,
            adjustment.ReferenceType,
            adjustment.ReferenceId,
            referenceName,
            promotion is { IsCodeBased: true } ? promotion.Code : null,
            adjustment.Type == PriceAdjustmentType.PackagePrice
                ? [
                    .. adjustment
                        .Allocations.OrderBy(x => x.OrderLineId)
                        .Select(x => new OrderPriceAllocationDto(x.OrderLineId, x.Amount)),
                ]
                : null
        );
    }

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
