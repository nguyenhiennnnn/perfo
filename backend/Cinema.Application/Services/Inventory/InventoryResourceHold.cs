using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class InventoryResourceHold(IAppDbContext db) : IResourceHold
{
    public string ResourceType => nameof(InventoryItem);

    public async Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken)
    {
        var order = request.Order;
        var comboLines = order
            .Lines.Where(l => l.LineType == OrderLineType.Combo && l.ComboId != null && l.Quantity > 0)
            .ToList();
        if (comboLines.Count == 0)
            return;

        var demands = comboLines
            .Select(l => new InventoryDemand(l.OrderLineId, l.ComboId!.Value, l.Quantity))
            .ToList();
        var required = await InventoryRequirements.BuildAsync(db, demands, cancellationToken);
        if (required.Count == 0)
            return;

        var lineById = comboLines.ToDictionary(l => l.OrderLineId);
        var now = DateTime.UtcNow;
        foreach (var group in required.GroupBy(r => r.InventoryItemId).OrderBy(g => g.Key))
        {
            var itemId = group.Key;
            var total = group.Sum(r => r.Quantity);
            var reserved = await db
                .InventoryItems.Where(i =>
                    i.InventoryItemId == itemId && i.QuantityOnHand - i.ReservedQuantity >= total
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity + total),
                    cancellationToken
                );
            if (reserved == 0)
                throw await InventoryRequirements.OutOfStockExceptionAsync(
                    db,
                    [.. group.Select(r => lineById[r.Key].ComboId!.Value).Distinct()],
                    itemId,
                    cancellationToken
                );

            foreach (var row in group)
                db.InventoryReservations.Add(
                    new InventoryReservation
                    {
                        OrderId = order.OrderId,
                        OrderLineId = row.Key,
                        InventoryItemId = itemId,
                        Quantity = row.Quantity,
                        Status = ResourceHoldStatus.Reserved,
                        CreatedAt = now,
                    }
                );
        }
    }

    public Task CommitAsync(int orderId, CancellationToken cancellationToken) =>
        ResolveAsync(orderId, ResourceHoldStatus.Committed, cancellationToken);

    public Task ReleaseAsync(int orderId, CancellationToken cancellationToken) =>
        ResolveAsync(orderId, ResourceHoldStatus.Released, cancellationToken);

    private async Task ResolveAsync(
        int orderId,
        ResourceHoldStatus target,
        CancellationToken cancellationToken
    )
    {
        var reserved = await db
            .InventoryReservations.AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Reserved)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.InventoryReservationId)
            .Select(r => new
            {
                r.InventoryReservationId,
                r.InventoryItemId,
                r.Quantity,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var reservation in reserved)
        {
            var claimed = await db
                .InventoryReservations.Where(r =>
                    r.InventoryReservationId == reservation.InventoryReservationId
                    && r.Status == ResourceHoldStatus.Reserved
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(r => r.Status, target)
                            .SetProperty(r => r.ResolvedAt, (DateTime?)now),
                    cancellationToken
                );
            if (claimed == 0)
                continue;

            var quantity = reservation.Quantity;
            await db
                .InventoryItems.IgnoreQueryFilters()
                .Where(i => i.InventoryItemId == reservation.InventoryItemId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - quantity),
                    cancellationToken
                );
        }
    }
}
