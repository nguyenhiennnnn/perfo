using Cinema.Application.DTOs.Orders;
using Cinema.Application.Events;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class TicketCheckInService(
    IAppDbContext db,
    IBusinessEventDispatcher eventDispatcher,
    ILogger<TicketCheckInService> logger
) : ITicketCheckInService
{
    public async Task<CheckinResponse> CheckInAsync(
        CheckinRequest request,
        CancellationToken cancellationToken
    )
    {
        var qrCode = request.QrCode.Trim();
        var orderId =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.QrCode == qrCode && l.LineType == OrderLineType.Ticket)
                .Select(l => (int?)l.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_QrCode);

        var checkInOpenMinutes = await GetCheckInOpenMinutesAsync(cancellationToken);
        CheckinResponse? response = null;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var line =
                    await db
                        .OrderLines.AsNoTracking()
                        .Include(l => l.Order)
                        .Include(l => l.TicketType)
                        .Include(l => l.ShowtimeSeat)
                        .ThenInclude(ss => ss!.Seat)
                        .Include(l => l.Showtime)
                        .ThenInclude(s => s!.Movie)
                        .Include(l => l.Showtime)
                        .ThenInclude(s => s!.Room)
                        .FirstOrDefaultAsync(
                            l => l.QrCode == qrCode && l.LineType == OrderLineType.Ticket,
                            cancellationToken
                        ) ?? throw new NotFoundException(Messages.NotFound_QrCode);

                EnsureOrderValid(line.Order.Status);

                if (line.IsCheckedIn)
                    throw new ConflictAppException(Messages.Conflict_TicketAlreadyCheckedIn);
                if (line.RefundedQuantity >= line.Quantity)
                    throw new BusinessRuleException(Messages.Rule_CheckInTicketRefunded);

                var showtime = line.Showtime!;
                if (showtime.Status == ShowtimeStatus.Cancelled)
                    throw new BusinessRuleException(Messages.Rule_ShowtimeCancelled);

                var now = DateTime.UtcNow;
                if (now < showtime.StartTime.AddMinutes(-checkInOpenMinutes))
                    throw new BusinessRuleException(
                        string.Format(Messages.Rule_CheckInNotOpenYet, checkInOpenMinutes)
                    );
                if (now > showtime.EndTime)
                    throw new BusinessRuleException(Messages.Rule_ShowtimeEnded);

                var checkedIn = await db
                    .OrderLines.Where(l =>
                        l.OrderLineId == line.OrderLineId
                        && !l.IsCheckedIn
                        && db.Orders.Any(o =>
                            o.OrderId == l.OrderId && o.Status == OrderStatus.Confirmed
                        )
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(l => l.IsCheckedIn, true)
                                .SetProperty(l => l.CheckedInAt, now),
                        cancellationToken
                    );
                if (checkedIn == 0)
                    throw new ConflictAppException(Messages.Conflict_TicketAlreadyCheckedIn);

                await eventDispatcher.PublishAsync(
                    new TicketCheckedIn(line.OrderId, line.OrderLineId, showtime.ShowtimeId, now),
                    cancellationToken
                );

                response = new CheckinResponse(
                    line.OrderLineId,
                    line.OrderId,
                    line.Order.OrderCode,
                    line.ShowtimeSeat!.Seat.SeatRow,
                    line.ShowtimeSeat.Seat.SeatNumber,
                    showtime.Movie.Title,
                    showtime.Room.RoomName,
                    showtime.StartTime,
                    now,
                    line.TicketTypeId,
                    line.TicketType?.Name ?? Messages.Common_TicketTypeStandard,
                    line.RequiresCardPresentation,
                    line.CardCheckStatus.ToString()
                );
            },
            cancellationToken
        );

        logger.LogInformation(
            "TicketCheckedIn OrderId {OrderId} OrderLineId {OrderLineId}",
            response!.OrderId,
            response.OrderLineId
        );
        return response;
    }

    private static void EnsureOrderValid(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.Confirmed:
                return;
            case OrderStatus.Cancelled:
                throw new BusinessRuleException(Messages.Rule_CheckInOrderCancelled);
            case OrderStatus.Expired:
                throw new BusinessRuleException(Messages.Rule_CheckInOrderExpired);
            default:
                throw new BusinessRuleException(Messages.Rule_CheckInOrderNotPaid);
        }
    }

    private async Task<int> GetCheckInOpenMinutesAsync(CancellationToken cancellationToken) =>
        await db
            .CinemaSettings.Where(s => s.Id == AppDbContextExtensions.CinemaSettingsId)
            .Select(s => (int?)s.CheckInOpenMinutes)
            .FirstOrDefaultAsync(cancellationToken)
        ?? CinemaSettings.DefaultCheckInOpenMinutes;
}
