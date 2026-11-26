using System.Globalization;
using Cinema.Application.BackgroundJobs;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
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
        IReadOnlyList<OrderTicketEmailLine> tickets,
        BookingComboSuggestionEmailData? comboSuggestions = null
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_BookingConfirmedSubject, _brand.Name),
            EmailTemplates.BookingConfirmed(
                _brand,
                customerName,
                bookingCode,
                movieTitle: movieTitle,
                tickets: tickets,
                comboSuggestions: comboSuggestions
            )
        );

    public void SendSupportRequestUpdated(string toEmail, SupportRequestUpdateEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_SupportUpdatedSubject, _brand.Name, data.SupportRequestId),
            EmailTemplates.SupportRequestUpdated(_brand, data)
        );

    public void SendOrderExchanged(string toEmail, OrderExchangeEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_OrderExchangedSubject, _brand.Name),
            EmailTemplates.OrderExchanged(_brand, data)
        );

    public void SendSupplementOrderReminder(string toEmail, SupplementReminderEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_SupplementReminderSubject, _brand.Name),
            EmailTemplates.SupplementOrderReminder(_brand, data)
        );

    public void SendMovieIntro(string toEmail, MovieIntroEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_MovieIntroSubject, _brand.Name),
            EmailTemplates.MovieIntro(_brand, data)
        );

    public void SendPointsExpiryReminder(string toEmail, PointsExpiryReminderEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_PointsExpiryReminderSubject, _brand.Name),
            EmailTemplates.PointsExpiryReminder(_brand, data)
        );

    public void SendLoyaltyChallengeCompleted(
        string toEmail,
        LoyaltyChallengeCompletedEmailData data
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_LoyaltyChallengeCompletedSubject, _brand.Name),
            EmailTemplates.LoyaltyChallengeCompleted(_brand, data)
        );

    public void SendShowtimeReminder(string toEmail, ShowtimeReminderEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_ShowtimeReminderSubject, _brand.Name),
            EmailTemplates.ShowtimeReminder(_brand, data)
        );

    public void SendReviewInvite(string toEmail, ReviewInviteEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_ReviewInviteSubject, _brand.Name),
            EmailTemplates.ReviewInvite(_brand, data)
        );

    public void SendWaitlistSeatsAvailable(string toEmail, WaitlistSeatsAvailableEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_WaitlistSeatsSubject, data.MovieTitle),
            EmailTemplates.WaitlistSeatsAvailable(_brand, data)
        );

    public void SendMovieOpenedForSale(string toEmail, MovieOpenedForSaleEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_MovieOpenedSubject, data.MovieTitle),
            EmailTemplates.MovieOpenedForSale(_brand, data)
        );

    public void SendInvoiceRequestProcessed(
        string toEmail,
        string customerName,
        string orderCode,
        InvoiceRequestStatus status,
        string? invoiceNumber,
        string? rejectReason
    ) =>
        Enqueue(
            toEmail,
            string.Format(
                status == InvoiceRequestStatus.Issued
                    ? Messages.Email_InvoiceIssuedSubject
                    : Messages.Email_InvoiceRejectedSubject,
                _brand.Name,
                orderCode
            ),
            EmailTemplates.InvoiceRequestProcessed(
                _brand,
                customerName,
                orderCode,
                status,
                invoiceNumber,
                rejectReason
            )
        );

    public void SendInvoiceAdjustmentNeeded(
        string toEmail,
        string orderCode,
        string invoiceNumber,
        decimal refundAmount
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_InvoiceAdjustmentSubject, _brand.Name, orderCode),
            EmailTemplates.InvoiceAdjustmentNeeded(_brand, orderCode, invoiceNumber, refundAmount)
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

    public void SendShowtimeRoomChangedNotice(
        string toEmail,
        string customerName,
        string orderCode,
        string movieTitle,
        DateTime showtimeStartUtc,
        string fromRoomName,
        string toRoomName,
        IReadOnlyList<string> seatLabels,
        decimal refundDueAmount
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_RoomChangedSubject, orderCode),
            EmailTemplates.ShowtimeRoomChanged(
                _brand,
                customerName,
                orderCode,
                movieTitle,
                showtimeStartUtc,
                fromRoomName,
                toRoomName,
                seatLabels,
                refundDueAmount
            )
        );

    public void SendGiftCardReceived(string toEmail, GiftCardEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_GiftCardReceivedSubject, data.PurchaserName, _brand.Name),
            EmailTemplates.GiftCardReceived(_brand, data)
        );

    public void SendGiftCardPurchaserConfirmation(string toEmail, GiftCardEmailData data) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_GiftCardPurchaserSubject, _brand.Name),
            EmailTemplates.GiftCardPurchaserConfirmation(_brand, data)
        );

    public void SendLowStockAlert(string toEmail, IReadOnlyList<LowStockEmailLine> items) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_LowStockSubject, _brand.Name, items.Count),
            EmailTemplates.LowStockAlert(_brand, items)
        );

    public void SendRoomQualityAlert(string toEmail, IReadOnlyList<RoomQualityEmailLine> rooms) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_RoomQualitySubject, _brand.Name, rooms.Count),
            EmailTemplates.RoomQualityAlert(_brand, rooms)
        );

    public void SendUnclosedShiftAlert(
        string toEmail,
        IReadOnlyList<UnclosedShiftEmailLine> shifts
    ) =>
        Enqueue(
            toEmail,
            string.Format(Messages.Email_UnclosedShiftSubject, _brand.Name, shifts.Count),
            EmailTemplates.UnclosedShiftAlert(_brand, shifts)
        );

    public void SendDailyClosingReport(string toEmail, DailyClosingEmailData report) =>
        Enqueue(
            toEmail,
            string.Format(
                Messages.Email_DailyClosingSubject,
                _brand.Name,
                report.BusinessDate.ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture)
            ),
            EmailTemplates.DailyClosingReport(_brand, report)
        );

    private void Enqueue(string toEmail, string subject, string html) =>
        backgroundJobClient.Enqueue<SendEmailJob>(j =>
            j.RunAsync(toEmail, subject, html, CancellationToken.None)
        );
}
