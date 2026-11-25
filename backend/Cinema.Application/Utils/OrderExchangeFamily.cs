using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class OrderExchangeFamily
{
    public static async Task<int> ResolveRootOrderIdAsync(
        this IAppDbContext db,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var current = orderId;
        while (true)
        {
            var parentId = await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.OrderId == current
                    && o.Type == OrderType.Exchange
                    && o.OriginalOrderId != null
                )
                .Select(o => o.OriginalOrderId)
                .FirstOrDefaultAsync(cancellationToken);
            if (parentId is not { } parent)
                return current;

            current = parent;
        }
    }

    public static async Task<List<int>> LoadExchangeFamilyOrderIdsAsync(
        this IAppDbContext db,
        int rootOrderId,
        CancellationToken cancellationToken
    )
    {
        var family = new List<int> { rootOrderId };
        var frontier = new List<int> { rootOrderId };
        while (frontier.Count > 0)
        {
            var current = frontier;
            frontier = await db
                .Orders.AsNoTracking()
                .Where(o =>
                    o.Type == OrderType.Exchange
                    && o.OriginalOrderId != null
                    && current.Contains(o.OriginalOrderId.Value)
                )
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);
            frontier = [.. frontier.Where(id => !family.Contains(id))];
            family.AddRange(frontier);
        }

        return family;
    }
}
