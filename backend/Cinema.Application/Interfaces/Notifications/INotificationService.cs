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

public sealed record LowStockEmailLine(
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal LowStockThreshold
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

public interface INotificationService
{
    void SendGiftCardReceived(string toEmail, GiftCardEmailData data);

    void SendGiftCardPurchaserConfirmation(string toEmail, GiftCardEmailData data);

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

    void SendOrderExchanged(string toEmail, OrderExchangeEmailData data);

    void SendSupplementOrderReminder(string toEmail, SupplementReminderEmailData data);

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

    void SendDailyClosingReport(string toEmail, DailyClosingEmailData report);
}
