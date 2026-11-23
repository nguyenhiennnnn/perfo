using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public sealed record InventoryDemand(int Key, int ComboId, int Quantity);

public sealed record InventoryRequirement(int Key, int InventoryItemId, decimal Quantity);

public static class InventoryRequirements
{
    public static async Task<List<InventoryRequirement>> BuildAsync(
        IAppDbContext db,
        IReadOnlyCollection<InventoryDemand> demands,
        CancellationToken cancellationToken
    )
    {
        if (demands.Count == 0)
            return [];

        var comboIds = demands.Select(d => d.ComboId).Distinct().ToList();
        var recipeLines = await db
            .ComboRecipeLines.AsNoTracking()
            .Where(l => comboIds.Contains(l.Recipe.ComboId))
            .Select(l => new
            {
                l.Recipe.ComboId,
                l.InventoryItemId,
                l.Quantity,
            })
            .ToListAsync(cancellationToken);
        var byCombo = recipeLines.ToLookup(l => l.ComboId);

        return
        [
            .. demands
                .SelectMany(d =>
                    byCombo[d.ComboId]
                        .Select(l => new InventoryRequirement(
                            d.Key,
                            l.InventoryItemId,
                            l.Quantity * d.Quantity
                        ))
                )
                .GroupBy(r => (r.Key, r.InventoryItemId))
                .Select(g => new InventoryRequirement(
                    g.Key.Key,
                    g.Key.InventoryItemId,
                    g.Sum(x => x.Quantity)
                )),
        ];
    }

    public static async Task<ConflictAppException> OutOfStockExceptionAsync(
        IAppDbContext db,
        IReadOnlyCollection<int> comboIds,
        int inventoryItemId,
        CancellationToken cancellationToken
    )
    {
        var names = await db
            .ComboRecipeLines.AsNoTracking()
            .Where(l =>
                l.InventoryItemId == inventoryItemId && comboIds.Contains(l.Recipe.ComboId)
            )
            .Select(l => l.Recipe.Combo.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
        return new ConflictAppException(
            string.Format(Messages.Conflict_ComboOutOfStock, string.Join(", ", names))
        );
    }

    public static IQueryable<Combo> InStock(this IQueryable<Combo> combos, IAppDbContext db) =>
        combos.Where(c =>
            !db.ComboRecipeLines.Any(l =>
                l.Recipe.ComboId == c.ComboId
                && l.InventoryItem.QuantityOnHand - l.InventoryItem.ReservedQuantity < l.Quantity
            )
        );
}
