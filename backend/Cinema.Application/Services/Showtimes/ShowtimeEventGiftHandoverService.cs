using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEventGiftHandoverService(
    IAppDbContext db,
    ILogger<ShowtimeEventGiftHandoverService> logger
) : IShowtimeEventGiftHandoverService
{
    public async Task<EventGiftHandoverTicketDto> LookupAsync(
        string qrCode,
        CancellationToken cancellationToken
    )
    {
        var (lineId, _) = await ResolveAsync(qrCode, cancellationToken);
        return await LoadAsync(lineId, cancellationToken);
    }

    public async Task<EventGiftHandoverResponse> HandOverAsync(
        int staffUserId,
        EventGiftHandoverRequest request,
        CancellationToken cancellationToken
    )
    {
        var (lineId, orderId) = await ResolveAsync(request.QrCode, cancellationToken);
        var now = DateTime.UtcNow;
        var handed = 0;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var line =
                    await db
                        .OrderLines.AsNoTracking()
                        .Where(l => l.OrderLineId == lineId)
                        .Select(l => new
                        {
                            OrderStatus = l.Order.Status,
                            ShowtimeStatus = l.Showtime!.Status,
                            Refunded = l.RefundedQuantity >= l.Quantity,
                            ExchangedOut = db.OrderExchanges.Any(e => e.OldOrderLineId == l.OrderLineId),
                        })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_QrCode);
                if (line.OrderStatus != OrderStatus.Confirmed)
                    throw new BusinessRuleException(Messages.Rule_EventGiftOrderNotConfirmed);
                if (line.Refunded || line.ExchangedOut)
                    throw new BusinessRuleException(Messages.Rule_EventGiftTicketInvalid);
                if (line.ShowtimeStatus == ShowtimeStatus.Cancelled)
                    throw new BusinessRuleException(Messages.Rule_ShowtimeCancelled);

                var reservations = await db
                    .ShowtimeEventGiftReservations.AsNoTracking()
                    .Where(r => r.OrderLineId == lineId && r.Status == ResourceHoldStatus.Committed)
                    .OrderBy(r => r.InventoryItemId)
                    .ThenBy(r => r.ShowtimeEventGiftReservationId)
                    .Select(r => new
                    {
                        r.ShowtimeEventGiftReservationId,
                        Remaining = r.Quantity - r.QuantityReturned - r.QuantityGiven,
                    })
                    .ToListAsync(cancellationToken);
                if (reservations.Count == 0)
                    throw new BusinessRuleException(Messages.Rule_EventGiftNoGifts);
                if (reservations.All(r => r.Remaining <= 0))
                    throw new ConflictAppException(Messages.Conflict_EventGiftAlreadyHandedOver);

                foreach (var reservation in reservations.Where(r => r.Remaining > 0))
                {
                    var id = reservation.ShowtimeEventGiftReservationId;
                    var quantity = reservation.Remaining;
                    var updated = await db
                        .ShowtimeEventGiftReservations.Where(r =>
                            r.ShowtimeEventGiftReservationId == id
                            && r.Status == ResourceHoldStatus.Committed
                            && r.Quantity - r.QuantityReturned - r.QuantityGiven >= quantity
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(r => r.QuantityGiven, r => r.QuantityGiven + quantity),
                            cancellationToken
                        );
                    if (updated == 0)
                        throw new ConflictAppException(Messages.Conflict_EventGiftAlreadyHandedOver);

                    db.ShowtimeEventGiftHandovers.Add(
                        new ShowtimeEventGiftHandover
                        {
                            ShowtimeEventGiftReservationId = id,
                            Quantity = quantity,
                            HandedByUserId = staffUserId,
                            HandedAt = now,
                        }
                    );
                    handed += quantity;
                }

                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "EventGiftHandedOver OrderLineId {OrderLineId} Quantity {Quantity} StaffUserId {StaffUserId}",
            lineId,
            handed,
            staffUserId
        );
        return new EventGiftHandoverResponse(await LoadAsync(lineId, cancellationToken), handed, now);
    }

    private async Task<(int LineId, int OrderId)> ResolveAsync(
        string qrCode,
        CancellationToken cancellationToken
    )
    {
        var code = qrCode.Trim();
        var line =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.LineType == OrderLineType.Ticket && l.QrCode == code)
                .Select(l => new { l.OrderLineId, l.OrderId })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_QrCode);
        return (line.OrderLineId, line.OrderId);
    }

    private async Task<EventGiftHandoverTicketDto> LoadAsync(
        int lineId,
        CancellationToken cancellationToken
    )
    {
        var line =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.OrderLineId == lineId)
                .Select(l => new
                {
                    l.OrderId,
                    l.Order.OrderCode,
                    OrderStatus = l.Order.Status,
                    l.Order.CustomerName,
                    ShowtimeId = l.ShowtimeId ?? 0,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_QrCode);

        var gifts = await db
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r => r.OrderLineId == lineId && r.Status == ResourceHoldStatus.Committed)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.ShowtimeEventGiftReservationId)
            .Select(r => new EventGiftHandoverLineDto(
                r.ShowtimeEventGiftReservationId,
                r.InventoryItemId,
                r.InventoryItem.Name,
                r.InventoryItem.Unit,
                r.Quantity - r.QuantityReturned,
                r.QuantityGiven,
                r.Quantity - r.QuantityReturned - r.QuantityGiven
            ))
            .ToListAsync(cancellationToken);

        var history = await db
            .ShowtimeEventGiftHandovers.AsNoTracking()
            .Where(h => h.Reservation.OrderLineId == lineId)
            .OrderBy(h => h.HandedAt)
            .ThenBy(h => h.ShowtimeEventGiftHandoverId)
            .Select(h => new EventGiftHandoverHistoryDto(
                h.ShowtimeEventGiftHandoverId,
                h.Reservation.InventoryItem.Name,
                h.Quantity,
                h.HandedAt,
                h.HandedByUserId,
                h.HandedByUser.FullName
            ))
            .ToListAsync(cancellationToken);

        return new EventGiftHandoverTicketDto(
            line.OrderId,
            line.OrderCode,
            lineId,
            line.ShowtimeId,
            line.OrderStatus.ToString(),
            line.CustomerName,
            gifts.Sum(g => Math.Max(0, g.Remaining)),
            gifts,
            history
        );
    }
}
