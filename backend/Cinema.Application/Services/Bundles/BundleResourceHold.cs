using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class BundleResourceHold(IAppDbContext db) : IResourceHold
{
    public string ResourceType => nameof(Bundle);

    public async Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken)
    {
        var order = request.Order;
        var demands = order
            .PriceAdjustments.Where(a =>
                a.Type == PriceAdjustmentType.PackagePrice
                && a.ReferenceType == nameof(Bundle)
                && a.ReferenceId != null
            )
            .GroupBy(a => a.ReferenceId!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new { BundleId = g.Key, Quantity = g.Count() })
            .ToList();
        if (demands.Count == 0)
            return;

        var showtimeId = order
            .Lines.Where(l => l.LineType == OrderLineType.Ticket && l.ShowtimeId != null)
            .Select(l => l.ShowtimeId!.Value)
            .FirstOrDefault();
        var startTime = showtimeId == 0
            ? null
            : await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => (DateTime?)s.StartTime)
                .FirstOrDefaultAsync(cancellationToken);
        if (startTime is not { } start)
            return;

        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var businessDate = BusinessDay.Of(start, settings.OpeningTime, settings.ClosingTime);
        var now = DateTime.UtcNow;

        foreach (var demand in demands)
        {
            var bundle = await db
                .Bundles.AsNoTracking()
                .Where(b => b.BundleId == demand.BundleId)
                .Select(b => new { b.DailyLimit })
                .FirstOrDefaultAsync(cancellationToken);
            if (bundle is null)
                throw new NotFoundException(Messages.NotFound_Bundle);
            if (bundle.DailyLimit is not { } limit)
                continue;

            await ConsumeAsync(demand.BundleId, businessDate, demand.Quantity, limit, cancellationToken);
            db.BundleReservations.Add(
                new BundleReservation
                {
                    OrderId = order.OrderId,
                    BundleId = demand.BundleId,
                    BusinessDate = businessDate,
                    Quantity = demand.Quantity,
                    Status = ResourceHoldStatus.Reserved,
                    CreatedAt = now,
                }
            );
        }
    }

    public Task CommitAsync(int orderId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return db
            .BundleReservations.IgnoreQueryFilters()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Reserved)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, ResourceHoldStatus.Committed)
                        .SetProperty(r => r.ResolvedAt, (DateTime?)now),
                cancellationToken
            );
    }

    public async Task ReleaseAsync(int orderId, CancellationToken cancellationToken)
    {
        var reserved = await db
            .BundleReservations.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == ResourceHoldStatus.Reserved)
            .OrderBy(r => r.BundleId)
            .ThenBy(r => r.BusinessDate)
            .Select(r => new
            {
                r.BundleReservationId,
                r.BundleId,
                r.BusinessDate,
                r.Quantity,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var reservation in reserved)
        {
            var claimed = await db
                .BundleReservations.IgnoreQueryFilters()
                .Where(r =>
                    r.BundleReservationId == reservation.BundleReservationId
                    && r.Status == ResourceHoldStatus.Reserved
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(r => r.Status, ResourceHoldStatus.Released)
                            .SetProperty(r => r.ResolvedAt, (DateTime?)now),
                    cancellationToken
                );
            if (claimed == 0)
                continue;

            var quantity = reservation.Quantity;
            await db
                .BundleDailyUsages.IgnoreQueryFilters()
                .Where(u =>
                    u.BundleId == reservation.BundleId
                    && u.BusinessDate == reservation.BusinessDate
                    && u.UsedCount >= quantity
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(u => u.UsedCount, u => u.UsedCount - quantity),
                    cancellationToken
                );
        }
    }

    private async Task ConsumeAsync(
        int bundleId,
        DateOnly businessDate,
        int quantity,
        int limit,
        CancellationToken cancellationToken
    )
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"BundleDailyUsages\" (\"BundleId\", \"BusinessDate\", \"UsedCount\") VALUES ({bundleId}, {businessDate}, 0) ON CONFLICT (\"BundleId\", \"BusinessDate\") DO NOTHING",
            cancellationToken
        );

        var consumed = await db
            .BundleDailyUsages.IgnoreQueryFilters()
            .Where(u =>
                u.BundleId == bundleId
                && u.BusinessDate == businessDate
                && u.UsedCount + quantity <= limit
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.UsedCount, u => u.UsedCount + quantity),
                cancellationToken
            );
        if (consumed == 0)
            throw new ConflictAppException(Messages.Conflict_BundleDailyLimitReached);
    }
}
