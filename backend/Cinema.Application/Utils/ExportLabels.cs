using Cinema.Domain.Enums;
using Cinema.Shared.Resources;

namespace Cinema.Application.Utils;

public static class ExportLabels
{
    public static string Order(OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending => Messages.Export_StatusPending,
            OrderStatus.Confirmed => Messages.Export_StatusConfirmed,
            OrderStatus.Cancelled => Messages.Export_StatusCancelled,
            _ => Messages.Export_StatusExpired,
        };

    public static string OrderKind(OrderType type) =>
        type switch
        {
            OrderType.Ticket => Messages.Export_OrderTypeTicket,
            OrderType.Combo => Messages.Export_OrderTypeCombo,
            OrderType.GiftCard => Messages.Export_OrderTypeGiftCard,
            OrderType.Membership => Messages.Export_OrderTypeMembership,
            OrderType.RoomRental => Messages.Export_OrderTypeRoomRental,
            _ => Messages.Export_OrderTypeSupplement,
        };

    public static string Channel(SalesChannel channel) =>
        channel == SalesChannel.Online
            ? Messages.Export_ChannelOnline
            : Messages.Export_ChannelCounter;

    public static string CancelReasonLabel(CancelReason? reason) =>
        reason switch
        {
            null => string.Empty,
            CancelReason.CustomerChangedMind => Messages.Export_CancelReasonCustomerChangedMind,
            CancelReason.DuplicateOrder => Messages.Export_CancelReasonDuplicateOrder,
            CancelReason.PaymentError => Messages.Export_CancelReasonPaymentError,
            CancelReason.ShowtimeCancelled => Messages.Export_CancelReasonShowtimeCancelled,
            CancelReason.RoomClosed => Messages.Export_CancelReasonRoomClosed,
            _ => Messages.Export_CancelReasonOther,
        };

    public static string CancelledByLabel(CancelledBy? by) =>
        by switch
        {
            null => string.Empty,
            CancelledBy.Customer => Messages.Export_CancelledByCustomer,
            CancelledBy.Staff => Messages.Export_CancelledByStaff,
            CancelledBy.Admin => Messages.Export_CancelledByAdmin,
            _ => Messages.Export_CancelledBySystem,
        };

    public static string Payment(PaymentStatus status) =>
        status switch
        {
            PaymentStatus.Pending => Messages.Export_StatusPending,
            PaymentStatus.Success => Messages.Export_StatusSuccess,
            _ => Messages.Export_StatusFailed,
        };

    public static string TransactionKind(PaymentTransactionType type) =>
        type == PaymentTransactionType.Charge
            ? Messages.Export_TypeCharge
            : Messages.Export_TypeRefund;

    public static string Method(PaymentMethod method) =>
        method == PaymentMethod.Cash ? Messages.Export_MethodCash : method.ToDtoMethod();

    public static string Showtime(ShowtimeStatus status) =>
        status switch
        {
            ShowtimeStatus.Scheduled => Messages.Export_StatusScheduled,
            ShowtimeStatus.Cancelled => Messages.Export_StatusCancelled,
            _ => Messages.Export_StatusCompleted,
        };
}
