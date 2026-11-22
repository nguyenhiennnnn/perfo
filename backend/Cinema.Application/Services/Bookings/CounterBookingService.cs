using Cinema.Application.DTOs.Bookings;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bookings;

public class CounterBookingService(
    IAppDbContext db,
    IBookingHoldService holdService,
    IBookingFulfillmentService fulfillmentService,
    IOrderCancellationService cancellationService,
    IBusinessEventDispatcher eventDispatcher
) : ICounterBookingService
{
    public async Task<CounterHoldResponse> CounterHoldSeatsAsync(
        int employeeId,
        int employeeUserId,
        CounterHoldRequest request,
        CancellationToken cancellationToken
    )
    {
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
        string customerName;
        string? customerPhone;
        if (request.UserId is not null)
        {
            var customer =
                await db
                    .Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_Customer);
            if (!customer.IsActive)
                throw new BusinessRuleException(Messages.Rule_CustomerAccountDisabled);
            if (customer.Role.RoleName != Roles.Customer)
                throw new BusinessRuleException(Messages.Rule_UserIsNotCustomer);
            customerName = customer.FullName;
            customerPhone = customer.Phone;
        }
        else
        {
            customerName = request.CustomerName;
            customerPhone = request.CustomerPhone;
        }

        var booking = await holdService.CreateHoldBookingAsync(
            request.ShowtimeId,
            request.SeatIds,
            request.Combos,
            holderUserId: employeeUserId,
            channel: BookingChannel.Counter,
            bookingUserId: request.UserId,
            createdByEmployeeId: employeeId,
            customerName: customerName,
            customerPhone: customerPhone,
            cancellationToken
        );

        return booking.Adapt<CounterHoldResponse>();
    }

    public Task<OrderCancellationResult> CounterCancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    ) => cancellationService.CancelAsync(orderId, actor, reason, cancellationToken);

    public async Task<CounterCheckoutResponse> CounterCheckoutAsync(
        int bookingId,
        CounterCheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var booking =
            await db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Booking);

        if (booking.Channel != BookingChannel.Counter)
            throw new BusinessRuleException(Messages.Rule_CounterPaymentOnlyForCounterBooking);
        if (booking.Status != BookingStatus.Pending || booking.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrHoldExpired);
        if (request.AmountReceived < booking.FinalAmount)
            throw new BusinessRuleException(Messages.Rule_AmountReceivedInsufficient);

        var change = request.AmountReceived - booking.FinalAmount;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Payments.Add(
            new Payment
            {
                BookingId = booking.BookingId,
                PaymentMethod = PaymentMethod.Cash,
                Status = PaymentStatus.Success,
                Amount = booking.FinalAmount,
                AmountReceived = request.AmountReceived,
                ChangeGiven = change,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync(cancellationToken);
        await fulfillmentService.ConfirmBookingAndGenerateTicketsAsync(
            bookingId,
            cancellationToken
        );
        await transaction.CommitAsync(cancellationToken);

        return new CounterCheckoutResponse(bookingId, "Confirmed", change);
    }

    public async Task<CheckinResponse> CheckinAsync(
        CheckinRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket =
            await db
                .BookingTickets.Include(t => t.ShowtimeSeat)
                    .ThenInclude(ss => ss.Seat)
                .Include(t => t.ShowtimeSeat)
                    .ThenInclude(ss => ss.Showtime)
                        .ThenInclude(s => s.Movie)
                .Include(t => t.ShowtimeSeat)
                    .ThenInclude(ss => ss.Showtime)
                        .ThenInclude(s => s.Room)
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.QrCode == request.QrCode, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_QrCode);

        if (ticket.Booking.Status != BookingStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_TicketNoLongerValid);

        if (ticket.IsCheckedIn)
            throw new ConflictAppException(Messages.Conflict_TicketAlreadyCheckedIn);

        var showtime = ticket.ShowtimeSeat.Showtime;
        if (showtime.Status == ShowtimeStatus.Cancelled)
            throw new BusinessRuleException(Messages.Rule_ShowtimeCancelled);
        var now = DateTime.UtcNow;

        var checkInOpenMinutes = await GetCheckInOpenMinutesAsync(cancellationToken);
        if (now < showtime.StartTime.AddMinutes(-checkInOpenMinutes))
            throw new BusinessRuleException(
                string.Format(Messages.Rule_CheckInNotOpenYet, checkInOpenMinutes)
            );
        if (now > showtime.EndTime)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEnded);

        await db.InTransactionAsync(
            async () =>
            {
                var checkedIn = await db
                    .BookingTickets.Where(t => t.TicketId == ticket.TicketId && !t.IsCheckedIn)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(t => t.IsCheckedIn, true)
                                .SetProperty(t => t.CheckedInAt, now),
                        cancellationToken
                    );
                if (checkedIn == 0)
                    throw new ConflictAppException(Messages.Conflict_TicketAlreadyCheckedIn);

                await eventDispatcher.PublishAsync(
                    new TicketCheckedIn(
                        ticket.BookingId,
                        ticket.TicketId,
                        showtime.ShowtimeId,
                        now
                    ),
                    cancellationToken
                );
            },
            cancellationToken
        );

        ticket.IsCheckedIn = true;
        ticket.CheckedInAt = now;

        return ticket.Adapt<CheckinResponse>();
    }

    private async Task<int> GetCheckInOpenMinutesAsync(CancellationToken cancellationToken) =>
        await db
            .CinemaSettings.Where(s => s.Id == AppDbContextExtensions.CinemaSettingsId)
            .Select(s => (int?)s.CheckInOpenMinutes)
            .FirstOrDefaultAsync(cancellationToken)
        ?? CinemaSettings.DefaultCheckInOpenMinutes;
}
