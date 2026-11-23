using Cinema.Application.BackgroundJobs;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Notifications;

public class NotificationService(
    IBackgroundJobClient backgroundJobClient,
    IOptions<BrandOptions> brandOptions
) : INotificationService
{
    private readonly BrandOptions _brand = brandOptions.Value;

    public void SendVerificationLink(
        VerificationTokenPurpose purpose,
        string toEmail,
        string fullName,
        string link,
        TimeSpan validFor
    )
    {
        var (subject, intro, button) = purpose switch
        {
            VerificationTokenPurpose.EmailConfirmation => (
                string.Format(Messages.Email_ConfirmEmailSubject, _brand.Name),
                Messages.Email_ConfirmEmailIntro,
                Messages.Email_ConfirmEmailButton
            ),
            VerificationTokenPurpose.PasswordReset => (
                string.Format(Messages.Email_PasswordResetSubject, _brand.Name),
                Messages.Email_PasswordResetIntro,
                Messages.Email_PasswordResetButton
            ),
            VerificationTokenPurpose.StaffInvite => (
                string.Format(Messages.Email_StaffInviteSubject, _brand.Name),
                Messages.Email_StaffInviteIntro,
                Messages.Email_StaffInviteButton
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
        };

        Enqueue(
            toEmail,
            subject,
            EmailTemplates.ActionLink(
                _brand,
                fullName,
                subject,
                intro,
                button,
                link,
                DateTimeUtils.FormatDuration(validFor)
            )
        );
    }

    public void SendBookingConfirmed(
        string toEmail,
        string customerName,
        string bookingCode,
        string movieTitle,
        IReadOnlyList<OrderTicketEmailLine> tickets
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_BookingConfirmedSubject, _brand.Name),
            EmailTemplates.BookingConfirmed(
                _brand,
                customerName,
                bookingCode,
                movieTitle: movieTitle,
                tickets: tickets
            )
        );

    public void SendOrderCancelled(
        string toEmail,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderTicketEmailLine> tickets,
        IReadOnlyList<OrderRefundEmailLine> refunds
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_OrderCancelledSubject, _brand.Name),
            EmailTemplates.OrderCancelled(_brand, customerName, orderCode, tickets, refunds)
        );

    public void SendSeatsExpiredToAdmin(
        string toEmail,
        bool autoRefunded,
        int bookingId,
        int paymentId,
        decimal amount
    ) =>
        Enqueue(
            toEmail,
            string.Format(
                autoRefunded
                    ? Messages.Email_SeatsExpiredAdminRefundedSubject
                    : Messages.Email_SeatsExpiredAdminManualRefundSubject,
                _brand.Name,
                bookingId
            ),
            EmailTemplates.SeatsExpiredAfterPayment(
                _brand,
                autoRefunded,
                bookingId,
                paymentId,
                amount
            )
        );

    public void SendSeatsExpiredToCustomer(
        string toEmail,
        string customerName,
        string bookingCode,
        bool autoRefunded,
        decimal amount
    ) =>
        Enqueue(
            toEmail,
            string.Format(
                autoRefunded
                    ? Messages.Email_SeatsExpiredCustomerRefundedSubject
                    : Messages.Email_SeatsExpiredCustomerPendingRefundSubject,
                _brand.Name,
                bookingCode
            ),
            EmailTemplates.SeatsExpiredAfterPaymentCustomer(
                _brand,
                customerName,
                bookingCode,
                autoRefunded,
                amount
            )
        );

    public void SendShowtimeCancelledNotice(
        string toEmail,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderRefundEmailLine> refunds
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_ShowtimeCancelledNoticeSubject, orderCode),
            EmailTemplates.ShowtimeCancelledNotice(_brand, customerName, orderCode, refunds)
        );

    public void SendShowtimeCancelledRefunded(
        string toEmail,
        string customerName,
        string orderCode,
        decimal refundAmount
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_ShowtimeCancelledRefundedSubject, orderCode),
            EmailTemplates.ShowtimeCancelledAutoRefunded(
                _brand,
                customerName,
                orderCode,
                refundAmount
            )
        );

    public void SendShowtimeCancelledManualRefund(
        string toEmail,
        string customerName,
        string orderCode,
        decimal amountDue
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_ShowtimeCancelledManualRefundSubject, orderCode),
            EmailTemplates.ShowtimeCancelledManualRefund(
                _brand,
                customerName,
                orderCode,
                amountDue
            )
        );

    private void Enqueue(string toEmail, string subject, string html) =>
        backgroundJobClient.Enqueue<SendEmailJob>(j =>
            j.RunAsync(toEmail, subject, html, CancellationToken.None)
        );
}
