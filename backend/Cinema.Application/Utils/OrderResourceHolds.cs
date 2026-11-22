using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class OrderResourceHolds
{
    public static OrderResourceHold Record(
        this IAppDbContext db,
        int orderId,
        string resourceType,
        int? resourceId,
        int quantity
    )
    {
        var hold = new OrderResourceHold
        {
            OrderId = orderId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Quantity = quantity,
            Status = ResourceHoldStatus.Reserved,
            CreatedAt = DateTime.UtcNow,
        };
        db.OrderResourceHolds.Add(hold);
        return hold;
    }

    public static async Task<List<OrderResourceHold>> ResolveAsync(
        this IAppDbContext db,
        int orderId,
        string resourceType,
        ResourceHoldStatus target,
        CancellationToken cancellationToken
    )
    {
        var holds = await db
            .OrderResourceHolds.Where(h =>
                h.OrderId == orderId
                && h.ResourceType == resourceType
                && h.Status == ResourceHoldStatus.Reserved
            )
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var hold in holds)
        {
            hold.Status = target;
            hold.ResolvedAt = now;
        }

        return holds;
    }
}
