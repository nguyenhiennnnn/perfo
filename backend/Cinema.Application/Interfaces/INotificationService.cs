using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public sealed record OrderRefundEmailLine(PaymentMethod Method, decimal Amount);

public sealed record OrderTicketEmailLine(
    string SeatLabel,
    string TicketTypeName,
    decimal PaidAmount,
    bool RequiresCardPresentation
);

public sealed record LowStockEmailLine(
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal LowStockThreshold
);

public interface INotificationService
{
    void SendVerificationLink(
        VerificationTokenPurpose purpose,
        string toEmail,
        string fullName,
        string link,
        TimeSpan validFor
    );

    void SendBookingConfirmed(
        string toEmail,
        string customerName,
        string bookingCode,
        string movieTitle,
        IReadOnlyList<OrderTicketEmailLine> tickets
    );

    void SendOrderCancelled(
        string toEmail,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderTicketEmailLine> tickets,
        IReadOnlyList<OrderRefundEmailLine> refunds
    );

    void SendSeatsExpiredToAdmin(
        string toEmail,
        bool autoRefunded,
        int bookingId,
        int paymentId,
        decimal amount
    );

    void SendSeatsExpiredToCustomer(
        string toEmail,
        string customerName,
        string bookingCode,
        bool autoRefunded,
        decimal amount
    );

    void SendShowtimeCancelledNotice(
        string toEmail,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderRefundEmailLine> refunds
    );

    void SendShowtimeCancelledRefunded(
        string toEmail,
        string customerName,
        string orderCode,
        decimal refundAmount
    );

    void SendShowtimeCancelledManualRefund(
        string toEmail,
        string customerName,
        string orderCode,
        decimal amountDue
    );

    void SendShowtimeRoomChangedNotice(
        string toEmail,
        string customerName,
        string orderCode,
        string movieTitle,
        DateTime showtimeStartUtc,
        string fromRoomName,
        string toRoomName,
        IReadOnlyList<string> seatLabels,
        decimal refundDueAmount
    );

    void SendLowStockAlert(string toEmail, IReadOnlyList<LowStockEmailLine> items);
}
