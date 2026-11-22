using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public sealed record OrderRefundEmailLine(PaymentMethod Method, decimal Amount);

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
        int seatCount
    );

    void SendOrderCancelled(
        string toEmail,
        string customerName,
        string orderCode,
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
}
