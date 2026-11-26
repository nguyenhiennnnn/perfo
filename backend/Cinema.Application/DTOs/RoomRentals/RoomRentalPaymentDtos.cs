using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public record RoomRentalPaymentOrderDto(
    int OrderId,
    string OrderCode,
    RoomRentalPaymentStage Stage,
    string Status,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTime? ExpiresAt
);

public record RoomRentalPaymentStageDto(
    RoomRentalPaymentStage Stage,
    decimal Amount,
    DateTime? DueAt,
    RoomRentalPaymentOrderDto? Order,
    bool IsPaid
);

public record RoomRentalPaymentsDto(
    int RequestId,
    string RequestCode,
    RoomRentalRequestStatus Status,
    decimal Total,
    decimal PaidTotal,
    List<RoomRentalPaymentStageDto> Stages
);

public record CreateRoomRentalPaymentOrderRequest(RoomRentalPaymentStage Stage);

public record RecordRoomRentalBankTransferRequest(
    RoomRentalPaymentStage Stage,
    decimal? Amount,
    string Reference,
    string? Note
);

public record RoomRentalTrackingCheckoutRequest(string Token, string PaymentMethod);

public record RoomRentalTrackingGiftCardRequest(string Token, string Code);
