using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEventGiftHold(IAppDbContext db) : IResourceHold
{
    public string ResourceType => nameof(ShowtimeEventGift);

    public async Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken)
    {
        var order = request.Order;
        var groups = order
            .Lines.Where(l =>
                l.LineType == OrderLineType.Ticket && l.ShowtimeId != null && l.Quantity > 0
            )
            .GroupBy(l => l.ShowtimeId!.Value)
            .OrderBy(g => g.Key)
            .ToList();
        if (groups.Count == 0)
            return;

        var showtimeIds = groups.Select(g => g.Key).ToList();
        var gifts = await db
            .ShowtimeEventGifts.AsNoTracking()
            .Where(g => showtimeIds.Contains(g.ShowtimeId))
            .Select(g => new
            {
                g.ShowtimeId,
                g.InventoryItemId,
                g.Quantity,
                g.QuantityPerTicket,
                g.InventoryItem.Name,
            })
            .ToListAsync(cancellationToken);
        if (gifts.Count == 0)
            return;

        var lineIds = groups.SelectMany(g => g.Select(l => l.OrderLineId)).ToList();
        var existing = (
            await db
                .ShowtimeEventGiftReservations.AsNoTracking()
                .Where(r => lineIds.Contains(r.OrderLineId))
                .Select(r => new { r.OrderLineId, r.InventoryItemId })
                .ToListAsync(cancellationToken)
        )
            .Select(r => (r.OrderLineId, r.InventoryItemId))
            .ToHashSet();

        var now = DateTime.UtcNow;
        foreach (var group in groups)
        {
            var showtimeGifts = gifts
                .Where(g => g.ShowtimeId == group.Key)
                .OrderBy(g => g.InventoryItemId)
                .ToList();
            if (showtimeGifts.Count == 0)
                continue;

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({ShowtimeEventLimitRules.LockClassId}, {group.Key})",
                cancellationToken
            );

            foreach (var gift in showtimeGifts)
            {
                var lines = group
                    .Where(l => !existing.Contains((l.OrderLineId, gift.InventoryItemId)))
                    .ToList();
                var need = lines.Sum(l => l.Quantity) * gift.QuantityPerTicket;
                if (need == 0)
                    continue;

                var held = await db
                    .Held(group.Key, gift.InventoryItemId)
                    .SumAsync(r => r.Quantity - r.QuantityReturned, cancellationToken);
                if (held + need > gift.Quantity)
                    throw new BusinessRuleException(
                        string.Format(
                            Messages.Rule_ShowtimeEventGiftSoldOut,
                            gift.Name,
                            Math.Max(0, gift.Quantity - held)
                        )
                    );

                var itemId = gift.InventoryItemId;
                var reserved = await db
                    .InventoryItems.Where(i =>
                        i.InventoryItemId == itemId && i.QuantityOnHand - i.ReservedQuantity >= need
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity + need),
                        cancellationToken
                    );
                if (reserved == 0)
                    throw new BusinessRuleException(
                        string.Format(Messages.Rule_ShowtimeEventGiftOutOfStock, gift.Name)
                    );

                foreach (var line in lines)
                    db.ShowtimeEventGiftReservations.Add(
                        new ShowtimeEventGiftReservation
                        {
                            OrderId = order.OrderId,
                            OrderLineId = line.OrderLineId,
                            ShowtimeId = group.Key,
                            InventoryItemId = itemId,
                            Quantity = line.Quantity * gift.QuantityPerTicket,
                            Status = ResourceHoldStatus.Reserved,
                            CreatedAt = now,
                        }
                    );
            }
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
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Reserved)
            .OrderBy(r => r.InventoryItemId)
            .ThenBy(r => r.ShowtimeEventGiftReservationId)
            .Select(r => new
            {
                r.ShowtimeEventGiftReservationId,
                r.InventoryItemId,
                r.Quantity,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var reservation in reserved)
        {
            var claimed = await db
                .ShowtimeEventGiftReservations.Where(r =>
                    r.ShowtimeEventGiftReservationId == reservation.ShowtimeEventGiftReservationId
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
