using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public enum RoomRentalCancelKind
{
    Customer,
    Venue,
    PaymentOverdue,
}

public record CancelRoomRentalRequest(bool VenueIncident, string Note);

public record RoomRentalCancellationResultDto(
    int RequestId,
    string RequestCode,
    RoomRentalRequestStatus Status,
    RoomRentalCancelKind Kind,
    int DepositRefundPercent,
    decimal RefundedAmount,
    decimal RetainedAmount,
    bool ShowtimeReleased
);
