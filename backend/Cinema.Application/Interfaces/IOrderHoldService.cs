using Cinema.Application.DTOs.Bookings;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public interface IOrderHoldService
{
    Task<HoldBookingResponse> HoldSeatsAsync(
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
        CancellationToken cancellationToken
    );
    Task EnsureWithinBookingLimitsAsync(
        int seatCount,
        List<ComboSelectionRequest>? combos,
        CancellationToken cancellationToken
    );
}
