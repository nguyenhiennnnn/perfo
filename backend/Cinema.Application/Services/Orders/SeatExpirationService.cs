using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class SeatExpirationService(
    IAppDbContext db,
    IOrderConfirmationService confirmationService,
    IOrderReleaseService releaseService,
    ILogger<SeatExpirationService> logger
) : ISeatExpirationService
{
    public async Task<int> ReleaseStaleHeldSeatsAsync(
        List<int>? showtimeIds,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;

        var query = db.ShowtimeSeats.Where(ss =>
            ss.Status == ShowtimeSeatStatus.Held && ss.HeldUntil != null && ss.HeldUntil < now
        );

        if (showtimeIds is not null)
        {
            if (showtimeIds.Count == 0)
                return 0;

            query = query.Where(ss => showtimeIds.Contains(ss.ShowtimeId));
        }

        return await query.ExecuteUpdateAsync(
            setters =>
                setters
                    .SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                    .SetProperty(x => x.HeldByUserId, (int?)null)
                    .SetProperty(x => x.HeldByOrderId, (int?)null)
                    .SetProperty(x => x.HeldUntil, (DateTime?)null),
            cancellationToken
        );
    }

    public async Task ExpireDueOrdersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var dueOrderIds = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Pending && o.ExpiresAt != null && o.ExpiresAt < now
            )
            .Select(o => o.OrderId)
            .ToListAsync(cancellationToken);

        foreach (var orderId in dueOrderIds)
        {
            try
            {
                var confirmation = await confirmationService.TryConfirmAsync(
                    orderId,
                    cancellationToken
                );
                if (
                    confirmation
                    is OrderConfirmationResult.Confirmed
                        or OrderConfirmationResult.AlreadyConfirmed
                )
                    continue;

                await releaseService.ExpireAsync(orderId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ExpireOrderFailed OrderId {OrderId}", orderId);
            }
        }

        var staleReleased = await ReleaseStaleHeldSeatsAsync(null, cancellationToken);
        if (staleReleased > 0)
            logger.LogInformation(
                "SeatsReleased (stale, không khớp đơn Pending nào) Count {Count}",
                staleReleased
            );
    }
}
