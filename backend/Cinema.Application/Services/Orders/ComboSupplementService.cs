using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Combos;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class ComboSupplementService(
    IAppDbContext db,
    IOrderHoldService holdService,
    IAppCache cache
) : IComboSupplementService
{
    public async Task<HoldOrderResponse> CreateAsync(
        int userId,
        int originalOrderId,
        ComboSupplementRequest request,
        CancellationToken cancellationToken
    )
    {
        var original =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == originalOrderId && o.UserId == userId)
                .Select(o => new
                {
                    o.Status,
                    o.CustomerName,
                    o.CustomerPhone,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (original.Status != OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_ComboSupplementOrderNotConfirmed);

        var showtimeStarts = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == originalOrderId
                && l.LineType == OrderLineType.Ticket
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeId != null
            )
            .ExcludingExchangedOut(db)
            .Select(l => l.Showtime!.StartTime)
            .ToListAsync(cancellationToken);
        if (showtimeStarts.Count == 0)
            throw new BusinessRuleException(Messages.Rule_ComboSupplementNoActiveTicket);

        var validUntil = showtimeStarts.Min();
        if (validUntil <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ComboSupplementShowtimeStarted);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var order = await holdService.CreateComboHoldOrderAsync(
            request.Combos,
            userId,
            SalesChannel.Online,
            userId,
            null,
            original.CustomerName,
            original.CustomerPhone,
            cancellationToken,
            supplement: new ComboSupplementHold(
                originalOrderId,
                settings.PreorderComboDiscountPercent,
                validUntil
            )
        );

        return new HoldOrderResponse(
            order.OrderId,
            order.OrderCode,
            order.Status.ToDtoStatus(),
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.ExpiresAt!.Value
        );
    }
}
