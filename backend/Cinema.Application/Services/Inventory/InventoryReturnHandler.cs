using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class InventoryReturnHandler(IAppDbContext db, IStockLedgerService stockLedger)
    : IInTransactionEventHandler<OrderCancelled>
{
    private const int QuantityScale = 3;

    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == businessEvent.OrderId
                && l.LineType == OrderLineType.Combo
                && l.Quantity > l.QuantityFulfilled
            )
            .Select(l => new
            {
                l.OrderLineId,
                l.Quantity,
                l.QuantityFulfilled,
            })
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            return;

        var lineIds = lines.Select(l => l.OrderLineId).ToList();
        var movements = await db
            .StockLedgers.AsNoTracking()
            .Where(l =>
                l.ReferenceType == StockReferenceTypes.OrderLine
                && (l.MovementType == StockMovementType.Sale || l.MovementType == StockMovementType.Return)
                && lineIds.Contains(l.ReferenceId!.Value)
            )
            .Select(l => new
            {
                LineId = l.ReferenceId!.Value,
                l.InventoryItemId,
                l.MovementType,
                l.QuantityChange,
            })
            .ToListAsync(cancellationToken);
        if (movements.Count == 0)
            return;

        var lineById = lines.ToDictionary(l => l.OrderLineId);
        var returns = movements
            .GroupBy(m => (m.LineId, m.InventoryItemId))
            .Select(g =>
            {
                var line = lineById[g.Key.LineId];
                var sold = -g.Where(m => m.MovementType == StockMovementType.Sale)
                    .Sum(m => m.QuantityChange);
                var returned = g.Where(m => m.MovementType == StockMovementType.Return)
                    .Sum(m => m.QuantityChange);
                var unfulfilled = line.Quantity - line.QuantityFulfilled;
                var due = Math.Round(
                    sold * unfulfilled / line.Quantity,
                    QuantityScale,
                    MidpointRounding.AwayFromZero
                );
                var amount = Math.Min(due, sold) - returned;
                return (g.Key.LineId, g.Key.InventoryItemId, Amount: amount);
            })
            .Where(r => r.Amount > 0m)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.LineId);

        foreach (var entry in returns)
            await stockLedger.PostAsync(
                new StockMovement(
                    entry.InventoryItemId,
                    StockMovementType.Return,
                    entry.Amount,
                    StockReferenceTypes.OrderLine,
                    entry.LineId
                ),
                cancellationToken
            );
    }
}
