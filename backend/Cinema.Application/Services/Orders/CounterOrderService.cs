using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class CounterOrderService(
    IAppDbContext db,
    IOrderHoldService holdService,
    IOrderCancellationService cancellationService
) : ICounterOrderService
{
    public async Task<CounterOrderResponse> CreateTicketOrderAsync(
        int employeeId,
        int employeeUserId,
        CounterHoldRequest request,
        CancellationToken cancellationToken
    )
    {
        var counterShiftId = await db.RequireOpenCounterShiftIdByEmployeeAsync(
            employeeId,
            cancellationToken
        );
        await holdService.EnsureWithinBookingLimitsAsync(
            request.SeatIds.Count,
            request.Combos,
            cancellationToken
        );
        var slots = await db.LoadSeatSlotsAsync(request.ShowtimeId, cancellationToken);
        if (
            SeatLayoutRules.ValidateSelection(slots, request.SeatIds, SeatGapRuleMode.Off) is
            { } seatError
        )
            throw new BusinessRuleException(seatError);

        var (customerName, customerPhone) = await ResolveCustomerAsync(
            request.UserId,
            request.CustomerName,
            request.CustomerPhone,
            cancellationToken
        );

        var order = await holdService.CreateHoldOrderAsync(
            request.ShowtimeId,
            request.SeatIds,
            request.Combos,
            holderUserId: employeeUserId,
            channel: SalesChannel.Counter,
            orderUserId: request.UserId,
            createdByEmployeeId: employeeId,
            customerName: customerName,
            customerPhone: customerPhone,
            cancellationToken,
            ticketSelections: request.Tickets,
            acceptPriceChange: request.AcceptPriceChange,
            counterShiftId: counterShiftId
        );

        return CounterOrderResponse.From(order);
    }

    public async Task<CounterOrderResponse> CreateComboOrderAsync(
        int employeeId,
        int employeeUserId,
        CounterComboOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var counterShiftId = await db.RequireOpenCounterShiftIdByEmployeeAsync(
            employeeId,
            cancellationToken
        );
        var (customerName, customerPhone) = await ResolveCustomerAsync(
            request.UserId,
            request.CustomerName,
            request.CustomerPhone,
            cancellationToken
        );

        var order = await holdService.CreateComboHoldOrderAsync(
            request.Combos,
            holderUserId: employeeUserId,
            channel: SalesChannel.Counter,
            orderUserId: request.UserId,
            createdByEmployeeId: employeeId,
            customerName: customerName,
            customerPhone: customerPhone,
            cancellationToken,
            counterShiftId: counterShiftId
        );

        return CounterOrderResponse.From(order);
    }

    public Task<OrderCancellationResult> CancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    ) => cancellationService.CancelAsync(orderId, actor, reason, cancellationToken);

    private async Task<(string Name, string? Phone)> ResolveCustomerAsync(
        int? userId,
        string customerName,
        string? customerPhone,
        CancellationToken cancellationToken
    )
    {
        if (userId is null)
            return (customerName, customerPhone);

        var customer =
            await db
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Customer);
        if (!customer.IsActive)
            throw new BusinessRuleException(Messages.Rule_CustomerAccountDisabled);
        if (customer.Role.RoleName != Roles.Customer)
            throw new BusinessRuleException(Messages.Rule_UserIsNotCustomer);

        return (customer.FullName, customer.Phone);
    }
}
