using Cinema.Application.DTOs.Orders;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalPaymentOrderService
{
    Task<RoomRentalPaymentsDto> GetAsync(int requestId, CancellationToken cancellationToken);

    Task<RoomRentalPaymentOrderDto> EnsureOrderAsync(
        int requestId,
        RoomRentalPaymentStage stage,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalBankTransferService
{
    Task<RoomRentalPaymentOrderDto> RecordAsync(
        int requestId,
        RecordRoomRentalBankTransferRequest request,
        int performedByUserId,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalTrackingPaymentService
{
    Task<RoomRentalPaymentsDto> GetAsync(string token, CancellationToken cancellationToken);

    Task<CheckoutResponse> CheckoutAsync(
        string token,
        RoomRentalPaymentStage stage,
        string paymentMethod,
        CancellationToken cancellationToken
    );

    Task<OrderGiftCardResponse> ApplyGiftCardAsync(
        string token,
        RoomRentalPaymentStage stage,
        int userId,
        string code,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalRevenueService
{
    Task<int> RecognizeDueAsync(CancellationToken cancellationToken);
}
