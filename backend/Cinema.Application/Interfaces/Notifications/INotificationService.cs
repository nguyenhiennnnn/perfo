using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Notifications;

public sealed record OrderRefundEmailLine(PaymentMethod Method, decimal Amount);

public sealed record OrderTicketEmailLine(
    string SeatLabel,
    string TicketTypeName,
    decimal PaidAmount,
    bool RequiresCardPresentation
);

public sealed record OrderExchangeEmailData(
    string CustomerName,
    string OrderCode,
    string ExchangeOrderCode,
    string MovieTitle,
    DateTime OldStartUtc,
    DateTime NewStartUtc,
    string NewRoomName,
    IReadOnlyList<OrderTicketEmailLine> Tickets,
    decimal AmountPaid,
    decimal CreditAmount,
    string? CreditCode,
    DateTime? CreditExpiresAtUtc
);

public enum SupplementReminderReason
{
    OriginalCancelled,
    OriginalExchanged,
}

public sealed record SupplementReminderComboLine(string ComboName, int Remaining);

public sealed record SupplementReminderOrderLine(
    string OrderCode,
    decimal Amount,
    IReadOnlyList<SupplementReminderComboLine> Combos
);

public sealed record SupplementReminderEmailData(
    string CustomerName,
    string OriginalOrderCode,
    SupplementReminderReason Reason,
    IReadOnlyList<SupplementReminderOrderLine> Supplements
);

public sealed record BookingComboSuggestionLine(
    string ComboName,
    int Quantity,
    decimal ListPrice,
    decimal PreorderPrice
);

public sealed record BookingComboSuggestionEmailData(
    int DiscountPercent,
    DateTime ValidUntilUtc,
    string PurchaseLink,
    IReadOnlyList<BookingComboSuggestionLine> Items
);

public sealed record ShowtimeReminderTicketLine(
    string SeatLabel,
    string TicketTypeName,
    string QrCode,
    bool RequiresCardPresentation
);

public sealed record ShowtimeReminderComboLine(
    string ComboName,
    int Remaining,
    string? SupplementOrderCode
);

public sealed record ShowtimeReminderEmailData(
    string CustomerName,
    string OrderCode,
    string MovieTitle,
    DateTime StartUtc,
    string RoomName,
    string CinemaAddress,
    int CheckInOpenMinutes,
    string TicketsLink,
    IReadOnlyList<ShowtimeReminderTicketLine> Tickets,
    IReadOnlyList<ShowtimeReminderComboLine> Combos
);

public sealed record ReviewInviteEmailData(
    string CustomerName,
    string MovieTitle,
    string ReviewLink,
    int RewardPoints
);

public sealed record WaitlistSeatsAvailableEmailData(
    string CustomerName,
    string MovieTitle,
    DateTime ShowtimeStartUtc,
    string RoomName,
    int SeatCount,
    string BookingLink,
    DateTime PriorityUntilUtc
);

public sealed record MovieOpenedForSaleEmailData(
    string CustomerName,
    string MovieTitle,
    DateTime FirstShowtimeStartUtc,
    string MovieLink
);

public sealed record LowStockEmailLine(
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal LowStockThreshold
);

public sealed record RoomQualityEmailLine(
    string RoomName,
    decimal AverageScore,
    int ConsecutiveDays,
    decimal Threshold,
    DateOnly FromDate,
    DateOnly ToDate
);

public sealed record UnclosedShiftEmailLine(
    int ShiftId,
    string EmployeeName,
    DateTime OpenedAtUtc,
    decimal OpeningCash
);

public sealed record DailyClosingEmailData(
    DateOnly BusinessDate,
    string ClosedByName,
    DateTime ClosedAtUtc,
    string? Note,
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal TotalRevenue,
    decimal GiftCardSold,
    decimal GiftCardUsed,
    decimal TotalDiscount,
    decimal TicketRefundAmount,
    int CancelledOrders,
    int AdjustmentCount,
    decimal AdjustmentRevenue,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Variance,
    int PendingOrders,
    int ProblemOrders
);

public sealed record GiftCardEmailData(
    string RecipientName,
    string RecipientEmail,
    string PurchaserName,
    string? Message,
    string Code,
    decimal Amount,
    DateTime ExpiresAtUtc
);

public sealed record PointsExpiryReminderLine(int Points, DateTime ExpiresAtUtc);

public sealed record PointsExpiryReminderEmailData(
    string CustomerName,
    int TotalPoints,
    string AccountLink,
    IReadOnlyList<PointsExpiryReminderLine> Lots
);

public sealed record LoyaltyChallengeCompletedEmailData(
    string CustomerName,
    string ChallengeName,
    int? RewardPoints,
    string? RewardName,
    string? PromoCode,
    DateTime? CodeExpiresAtUtc,
    string AccountLink
);

public sealed record SupportApologyCodeEmailData(
    string Code,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    DateTime ExpiresAtUtc
);

public sealed record SupportRequestUpdateEmailData(
    string CustomerName,
    int SupportRequestId,
    string Subject,
    string OrderCode,
    SupportRequestStatus Status,
    bool StatusChanged,
    string? StaffMessage,
    SupportApologyCodeEmailData? ApologyCode,
    string OrderLink
);

public sealed record MovieIntroEmailShowtimeLine(
    DateTime StartUtc,
    string RoomName,
    decimal? FromPrice
);

public sealed record MovieIntroEmailMovieLine(
    string Title,
    string Reason,
    string MovieLink,
    IReadOnlyList<MovieIntroEmailShowtimeLine> Showtimes
);

public sealed record MovieIntroEmailData(
    string CustomerName,
    IReadOnlyList<MovieIntroEmailMovieLine> Movies,
    string UnsubscribeLink,
    string PreferencesLink
);

public sealed record RoomRentalRequestReceivedEmailData(
    string CustomerName,
    string RequestCode,
    DateTime DesiredStartUtc,
    int AttendeeCount,
    string TrackingLink,
    DateTime LinkExpiresAtUtc
);

public interface INotificationService
{
    void SendRoomRentalRequestReceived(string toEmail, RoomRentalRequestReceivedEmailData data);

    void SendMovieIntro(string toEmail, MovieIntroEmailData data);

    void SendSupportRequestUpdated(string toEmail, SupportRequestUpdateEmailData data);

    void SendLoyaltyChallengeCompleted(string toEmail, LoyaltyChallengeCompletedEmailData data);

    void SendPointsExpiryReminder(string toEmail, PointsExpiryReminderEmailData data);

    void SendGiftCardReceived(string toEmail, GiftCardEmailData data);

    void SendGiftCardPurchaserConfirmation(string toEmail, GiftCardEmailData data);

    void SendVerificationLink(
        VerificationTokenPurpose purpose,
        string toEmail,
        string fullName,
        string link,
        TimeSpan validFor
    );

    void SendVerificationCode(string toEmail, string fullName, string code, TimeSpan validFor);

    void SendBookingConfirmed(
        string toEmail,
        string customerName,
        string bookingCode,
        string movieTitle,
        IReadOnlyList<OrderTicketEmailLine> tickets,
        BookingComboSuggestionEmailData? comboSuggestions = null
    );

    void SendOrderExchanged(string toEmail, OrderExchangeEmailData data);

    void SendSupplementOrderReminder(string toEmail, SupplementReminderEmailData data);

    void SendShowtimeReminder(string toEmail, ShowtimeReminderEmailData data);

    void SendReviewInvite(string toEmail, ReviewInviteEmailData data);

    void SendWaitlistSeatsAvailable(string toEmail, WaitlistSeatsAvailableEmailData data);

    void SendMovieOpenedForSale(string toEmail, MovieOpenedForSaleEmailData data);

    void SendInvoiceRequestProcessed(
        string toEmail,
        string customerName,
        string orderCode,
        InvoiceRequestStatus status,
        string? invoiceNumber,
        string? rejectReason
    );

    void SendInvoiceAdjustmentNeeded(
        string toEmail,
        string orderCode,
        string invoiceNumber,
        decimal refundAmount
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

    void SendUnclosedShiftAlert(string toEmail, IReadOnlyList<UnclosedShiftEmailLine> shifts);

    void SendRoomQualityAlert(string toEmail, IReadOnlyList<RoomQualityEmailLine> rooms);

    void SendDailyClosingReport(string toEmail, DailyClosingEmailData report);
}
