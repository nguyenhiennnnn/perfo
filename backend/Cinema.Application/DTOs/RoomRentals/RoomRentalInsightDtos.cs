using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalCalendarItemDto(
    int ShowtimeId,
    int RoomId,
    string RoomName,
    DateTime StartTime,
    DateTime EndTime,
    string? ContentTitle,
    ShowtimeStatus ShowtimeStatus,
    int RequestId,
    string RequestCode,
    string Customer,
    RoomRentalRequestStatus RequestStatus,
    int AttendeeCount
);

public record RoomRentalCancelPreviewDto(
    int RequestId,
    string RequestCode,
    RoomRentalRequestStatus Status,
    bool CanCancel,
    RoomRentalCancelKind Kind,
    DateTime? StartAt,
    double HoursBeforeStart,
    int DepositRefundPercent,
    decimal DepositPaid,
    decimal BalancePaid,
    decimal EstimatedRefund,
    decimal EstimatedRetained
);

public record RoomRentalReportStatusDto(RoomRentalRequestStatus Status, int Count);

public record RoomRentalReportDailyDto(DateOnly BusinessDate, decimal Revenue, int Events);

public record RoomRentalReportDto(
    DateOnly From,
    DateOnly To,
    int TotalRequests,
    int QuotedRequests,
    int ClosedRequests,
    int CancelledRequests,
    decimal CloseRate,
    decimal RecognizedRevenue,
    decimal PendingRevenue,
    List<RoomRentalReportStatusDto> ByStatus,
    List<RoomRentalReportDailyDto> Daily
);
