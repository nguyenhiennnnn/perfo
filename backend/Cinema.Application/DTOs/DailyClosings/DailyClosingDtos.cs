using System.Text.Json;
using Cinema.Application.DTOs.Reports;

namespace Cinema.Application.DTOs.DailyClosings;

public record DailyClosingRevenueDto(
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal TotalRevenue,
    decimal GiftCardSold,
    decimal GiftCardUsed
);

public record DailyClosingDiscountDto(
    decimal TierDiscount,
    decimal PromotionDiscount,
    decimal OtherDiscount,
    decimal TotalDiscount
);

public record DailyClosingRefundDto(
    decimal PaymentRefunded,
    decimal TicketRefundAmount,
    int CancelledOrders
);

public record DailyClosingMethodDto(
    string Channel,
    string? Method,
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal GiftCardSold,
    decimal Collected,
    decimal Refunded,
    int TransactionCount
);

public record DailyClosingOrderDto(int PendingOrders, int ProblemOrders);

public record DailyClosingShiftDto(
    int CounterShiftId,
    string EmployeeName,
    string Status,
    decimal OpeningCash,
    decimal ExpectedCash,
    decimal? CountedCash,
    decimal? Variance
);

public record DailyClosingCashDto(
    int ShiftCount,
    int OpenShiftCount,
    decimal OpeningCash,
    decimal Collected,
    decimal Refunded,
    decimal ChangeGiven,
    decimal ExpectedCash,
    IReadOnlyList<DailyClosingShiftDto> Shifts
);

public record DailyClosingSnapshot(
    DateOnly BusinessDate,
    DateTime GeneratedAt,
    DailyClosingRevenueDto Revenue,
    DailyClosingDiscountDto Discounts,
    DailyClosingRefundDto Refunds,
    IReadOnlyList<DailyClosingMethodDto> Methods,
    DailyClosingOrderDto Orders,
    DailyClosingCashDto Cash,
    IReadOnlyList<ReportBlockResult> Blocks
);

public record CloseDailyClosingRequest(
    decimal CountedCash,
    string? Note,
    bool OverrideBlockers = false
);

public record DailyClosingListFilter(DateOnly? FromDate, DateOnly? ToDate);

public record DailyClosingListItemDto(
    int DailyClosingId,
    DateOnly BusinessDate,
    string Status,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Variance,
    string? Note,
    int ClosedByUserId,
    string ClosedByName,
    DateTime ClosedAt
);

public record DailyClosingDto(DailyClosingListItemDto Closing, JsonElement Snapshot);

public enum DailyClosingReportMode
{
    OriginalDate,
    PostingDate,
}

public record DailyClosingAdjustmentDto(
    int LedgerAdjustmentId,
    DateOnly PostingDate,
    DateOnly OriginalDate,
    string Type,
    decimal TicketRevenueDelta,
    decimal ComboRevenueDelta,
    decimal GiftCardSoldDelta,
    decimal DiscountDelta,
    decimal RefundAmountDelta,
    int CancelledOrdersDelta,
    string Reason,
    int? OrderId,
    int? PaymentTransactionId,
    DateTime CreatedAt
);

public record DailyClosingAdjustmentTotalsDto(
    int Count,
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal GiftCardSold,
    decimal Discount,
    decimal RefundAmount,
    int CancelledOrders
);

public record DailyClosingFiguresDto(
    decimal TicketRevenue,
    decimal ComboRevenue,
    decimal TotalRevenue,
    decimal GiftCardSold,
    decimal GiftCardUsed,
    decimal TotalDiscount,
    decimal TicketRefundAmount,
    int CancelledOrders
);

public record DailyClosingReportDto(
    string Mode,
    DateOnly BusinessDate,
    bool IsClosed,
    DailyClosingListItemDto? Closing,
    DailyClosingSnapshot Snapshot,
    DailyClosingFiguresDto Base,
    IReadOnlyList<DailyClosingAdjustmentDto> Adjustments,
    DailyClosingAdjustmentTotalsDto AdjustmentTotals,
    DailyClosingFiguresDto Adjusted
);
