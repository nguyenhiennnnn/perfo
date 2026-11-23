using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class StockLedgerService(IAppDbContext db, ICurrentActorAccessor actorAccessor)
    : IStockLedgerService
{
    public async Task<StockPosting> PostAsync(
        StockMovement movement,
        CancellationToken cancellationToken
    )
    {
        if (movement.QuantityChange == 0)
            throw new ArgumentOutOfRangeException(nameof(movement));

        StockPosting? posting = null;
        await db.InTransactionAsync(
            async () => posting = await PostCoreAsync(movement, cancellationToken),
            cancellationToken
        );
        return posting!;
    }

    private async Task<StockPosting> PostCoreAsync(
        StockMovement movement,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var delta = movement.QuantityChange;
        var itemId = movement.InventoryItemId;

        var keepsReserved = delta < 0 && movement.MovementType != StockMovementType.Sale;
        var items = db.InventoryItems.IgnoreQueryFilters().Where(i => i.InventoryItemId == itemId);
        var candidate = keepsReserved
            ? items.Where(i => i.QuantityOnHand + delta >= i.ReservedQuantity)
            : items.Where(i => i.QuantityOnHand + delta >= 0);

        var affected = movement.NewUnitCost is { } newCost
            ? await candidate.ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand + delta)
                        .SetProperty(i => i.UnitCost, newCost)
                        .SetProperty(i => i.UpdatedAt, (DateTime?)now),
                cancellationToken
            )
            : await candidate.ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand + delta)
                        .SetProperty(i => i.UpdatedAt, (DateTime?)now),
                cancellationToken
            );

        if (affected == 0)
        {
            var exists = await db
                .InventoryItems.IgnoreQueryFilters()
                .AnyAsync(i => i.InventoryItemId == itemId, cancellationToken);
            throw exists
                ? new BusinessRuleException(
                    keepsReserved
                        ? Messages.Rule_InventoryStockReserved
                        : Messages.Rule_InventoryInsufficientStock
                )
                : new NotFoundException(Messages.NotFound_InventoryItem);
        }

        var state = await db
            .InventoryItems.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.InventoryItemId == itemId)
            .Select(i => new { i.QuantityOnHand, i.UnitCost })
            .FirstAsync(cancellationToken);

        var entry = new StockLedger
        {
            InventoryItemId = itemId,
            MovementType = movement.MovementType,
            QuantityChange = delta,
            BalanceAfter = state.QuantityOnHand,
            UnitCost = state.UnitCost,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            Reason = movement.Reason,
            ActorUserId = actorAccessor.Current?.UserId,
            OccurredAt = now,
        };
        db.StockLedgers.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return new StockPosting(entry.StockLedgerId, state.QuantityOnHand, state.UnitCost);
    }
}
