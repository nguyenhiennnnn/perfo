using Cinema.Application.DTOs.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public interface IOrderHoldService
{
    Task<HoldOrderResponse> HoldSeatsAsync(
        int userId,
        HoldSeatsRequest request,
        CancellationToken cancellationToken
    );
    Task<Order> CreateHoldOrderAsync(
        int showtimeId,
        List<int> showtimeSeatIds,
        List<ComboSelectionRequest>? comboSelections,
        int holderUserId,
        SalesChannel channel,
        int? orderUserId,
        int? createdByEmployeeId,
        string customerName,
        string? customerPhone,
        CancellationToken cancellationToken,
        IReadOnlyList<SeatTicketSelectionRequest>? ticketSelections = null,
        bool acceptPriceChange = false
    );
    Task<Order> CreateComboHoldOrderAsync(
        List<ComboSelectionRequest> comboSelections,
        int holderUserId,
        SalesChannel channel,
        int? orderUserId,
        int? createdByEmployeeId,
        string customerName,
        string? customerPhone,
        CancellationToken cancellationToken
    );
    Task EnsureWithinBookingLimitsAsync(
        int seatCount,
        List<ComboSelectionRequest>? combos,
        CancellationToken cancellationToken
    );
}
