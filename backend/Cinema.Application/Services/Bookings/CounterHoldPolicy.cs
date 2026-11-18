using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bookings;

public class CounterHoldPolicy(IAppDbContext db) : IBookingHoldPolicy
{
    private const int MaxPendingPerEmployee = 5;

    public BookingChannel Channel => BookingChannel.Counter;

    public void EnsureSalesOpen(Showtime showtime, int stopSellingMinutesBefore) { }

    public async Task EnsureNoConflictingPendingAsync(
        BookingHoldPolicyContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.CreatedByEmployeeId is null)
            return;

        var pendingCount = await db.Bookings.CountAsync(
            b =>
                b.CreatedByEmployeeId == context.CreatedByEmployeeId
                && b.Status == BookingStatus.Pending
                && b.ExpiresAt > DateTime.UtcNow
                && b.Channel == BookingChannel.Counter,
            cancellationToken
        );

        if (pendingCount >= MaxPendingPerEmployee)
            throw new ConflictAppException(Messages.Rule_CounterPendingLimit);
    }
}
