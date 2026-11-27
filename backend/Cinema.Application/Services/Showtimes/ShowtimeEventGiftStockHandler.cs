using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Inventory;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEventGiftStockHandler(IAppDbContext db, IStockLedgerService stockLedger)
    : IInTransactionEventHandler<OrderConfirmed>,
        IInTransactionEventHandler<OrderRefunded>,
        IInTransactionEventHandler<OrderCancelled>,
        IInTransactionEventHandler<OrderExchanged>
{
    public Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        PostSalesAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken) =>
        ReturnUngivenAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken) =>
        ReturnUngivenAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderExchanged businessEvent, CancellationToken cancellationToken) =>
        ReturnUngivenAsync(businessEvent.OrderId, cancellationToken);

    private async Task PostSalesAsync(int orderId, CancellationToken cancellationToken)
    {
        var reservations = await db
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Committed)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.ShowtimeEventGiftReservationId)
            .Select(r => new
            {
                r.ShowtimeEventGiftReservationId,
                r.InventoryItemId,
                r.Quantity,
            })
            .ToListAsync(cancellationToken);
        if (reservations.Count == 0)
            return;

        var ids = reservations.Select(r => r.ShowtimeEventGiftReservationId).ToList();
        var posted = (
            await db
                .StockLedgers.AsNoTracking()
                .Where(l =>
                    l.ReferenceType == StockReferenceTypes.EventGiftReservation
                    && l.MovementType == StockMovementType.Sale
                    && ids.Contains(l.ReferenceId!.Value)
                )
                .Select(l => l.ReferenceId!.Value)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        foreach (var reservation in reservations)
        {
            if (posted.Contains(reservation.ShowtimeEventGiftReservationId))
                continue;

            await stockLedger.PostAsync(
                new StockMovement(
                    reservation.InventoryItemId,
                    StockMovementType.Sale,
                    -reservation.Quantity,
                    StockReferenceTypes.EventGiftReservation,
                    reservation.ShowtimeEventGiftReservationId
                ),
                cancellationToken
            );
        }
    }

    private async Task ReturnUngivenAsync(int orderId, CancellationToken cancellationToken)
    {
        var rows = await db
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Committed)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.ShowtimeEventGiftReservationId)
            .Select(r => new
            {
                r.ShowtimeEventGiftReservationId,
                r.InventoryItemId,
                r.Quantity,
                r.QuantityGiven,
                r.QuantityReturned,
                LineQuantity = r.OrderLine.Quantity,
                LineRefunded = r.OrderLine.RefundedQuantity,
                Cancelled = r.Order.Status == OrderStatus.Cancelled,
                ExchangedOut = db.OrderExchanges.Any(e => e.OldOrderLineId == r.OrderLineId),
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.LineQuantity <= 0)
                continue;

            var perTicket = row.Quantity / row.LineQuantity;
            var entitled =
                row.Cancelled || row.ExchangedOut
                    ? 0
                    : Math.Max(0, row.LineQuantity - row.LineRefunded) * perTicket;
            var held = row.Quantity - row.QuantityReturned;
            var giveBack = held - Math.Max(entitled, row.QuantityGiven);
            if (giveBack <= 0)
                continue;

            var id = row.ShowtimeEventGiftReservationId;
            var returnedBefore = row.QuantityReturned;
            var claimed = await db
                .ShowtimeEventGiftReservations.Where(r =>
                    r.ShowtimeEventGiftReservationId == id
                    && r.QuantityReturned == returnedBefore
                    && r.Quantity - r.QuantityReturned - giveBack >= r.QuantityGiven
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(r => r.QuantityReturned, r => r.QuantityReturned + giveBack),
                    cancellationToken
                );
            if (claimed == 0)
                continue;

            await stockLedger.PostAsync(
                new StockMovement(
                    row.InventoryItemId,
                    StockMovementType.Return,
                    giveBack,
                    StockReferenceTypes.EventGiftReservation,
                    id
                ),
                cancellationToken
            );
        }
    }
}
