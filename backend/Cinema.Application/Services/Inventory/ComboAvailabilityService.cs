using Cinema.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class ComboAvailabilityService(IAppDbContext db) : IComboAvailabilityService
{
    public async Task EnsureAvailableAsync(
        IReadOnlyCollection<(int ComboId, int Quantity)> selections,
        CancellationToken cancellationToken
    )
    {
        if (selections.Count == 0)
            return;

        var demands = selections
            .Select((s, index) => new InventoryDemand(index, s.ComboId, s.Quantity))
            .ToList();
        var required = await InventoryRequirements.BuildAsync(db, demands, cancellationToken);
        if (required.Count == 0)
            return;

        var totals = required
            .GroupBy(r => r.InventoryItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var itemIds = totals.Keys.ToList();
        var available = await db
            .InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .Select(i => new { i.InventoryItemId, Available = i.QuantityOnHand - i.ReservedQuantity })
            .ToDictionaryAsync(i => i.InventoryItemId, i => i.Available, cancellationToken);

        var shortItemId = totals
            .OrderBy(t => t.Key)
            .Where(t => available.GetValueOrDefault(t.Key) < t.Value)
            .Select(t => (int?)t.Key)
            .FirstOrDefault();
        if (shortItemId is null)
            return;

        var comboIds = demands.Select(d => d.ComboId).Distinct().ToList();
        throw await InventoryRequirements.OutOfStockExceptionAsync(
            db,
            comboIds,
            shortItemId.Value,
            cancellationToken
        );
    }
}
