using Cinema.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public static class InventoryLocks
{
    public static Task<int> LockInventoryItemsAsync(
        this IAppDbContext db,
        int[] inventoryItemIds,
        CancellationToken cancellationToken
    ) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"InventoryItems\" WHERE \"InventoryItemId\" = ANY({inventoryItemIds}) ORDER BY \"InventoryItemId\" FOR UPDATE",
            cancellationToken
        );
}
