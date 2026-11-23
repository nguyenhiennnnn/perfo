using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class InventorySaleHandler(IAppDbContext db, IStockLedgerService stockLedger)
    : IInTransactionEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken)
    {
        var reservations = await db
            .InventoryReservations.AsNoTracking()
            .Where(r =>
                r.OrderId == businessEvent.OrderId && r.Status == ResourceHoldStatus.Committed
            )
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.OrderLineId)
            .Select(r => new
            {
                r.OrderLineId,
                r.InventoryItemId,
                r.Quantity,
            })
            .ToListAsync(cancellationToken);
        if (reservations.Count == 0)
            return;

        var lineIds = reservations.Select(r => r.OrderLineId).Distinct().ToList();
        var posted = await db
            .StockLedgers.AsNoTracking()
            .Where(l =>
                l.ReferenceType == StockReferenceTypes.OrderLine
                && l.MovementType == StockMovementType.Sale
                && lineIds.Contains(l.ReferenceId!.Value)
            )
            .Select(l => new { LineId = l.ReferenceId!.Value, l.InventoryItemId })
            .ToListAsync(cancellationToken);
        var postedKeys = posted.Select(p => (p.LineId, p.InventoryItemId)).ToHashSet();

        var lineCosts = new Dictionary<int, decimal>();
        foreach (var reservation in reservations)
        {
            if (postedKeys.Contains((reservation.OrderLineId, reservation.InventoryItemId)))
                continue;

            var posting = await stockLedger.PostAsync(
                new StockMovement(
                    reservation.InventoryItemId,
                    StockMovementType.Sale,
                    -reservation.Quantity,
                    StockReferenceTypes.OrderLine,
                    reservation.OrderLineId
                ),
                cancellationToken
            );
            lineCosts[reservation.OrderLineId] =
                lineCosts.GetValueOrDefault(reservation.OrderLineId)
                + reservation.Quantity * posting.UnitCost;
        }

        if (lineCosts.Count == 0)
            return;

        var costedLineIds = lineCosts.Keys.ToList();
        var quantities = await db
            .OrderLines.AsNoTracking()
            .Where(l => costedLineIds.Contains(l.OrderLineId))
            .Select(l => new { l.OrderLineId, l.Quantity })
            .ToListAsync(cancellationToken);
        foreach (var line in quantities)
        {
            var unitCost = OrderPricing.RoundToDong(lineCosts[line.OrderLineId] / line.Quantity);
            await db
                .OrderLines.Where(l => l.OrderLineId == line.OrderLineId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(l => l.UnitCost, (decimal?)unitCost),
                    cancellationToken
                );
        }
    }
}
