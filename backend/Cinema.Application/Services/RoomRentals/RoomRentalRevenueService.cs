using Cinema.Application.Caching;
using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalRevenueService(
    IAppDbContext db,
    IAppCache cache,
    IPaymentLedgerService ledger,
    ILedgerAdjustmentWriter adjustmentWriter,
    ISalesStatsService salesStats,
    IAuditLogger auditLogger,
    ILogger<RoomRentalRevenueService> logger
) : IRoomRentalRevenueService
{
    private const int BatchSize = 100;

    private sealed record DueShowtime(
        int ShowtimeId,
        int RequestId,
        string RequestCode,
        DateTime StartTime
    );

    public async Task<int> RecognizeDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var total = 0;
        var failed = new HashSet<int>();

        while (true)
        {
            var now = DateTime.UtcNow;
            var due = await db
                .Showtimes.AsNoTracking()
                .Where(s =>
                    s.Kind == ShowtimeKind.Private
                    && s.RoomRentalRequestId != null
                    && s.Status != ShowtimeStatus.Cancelled
                    && s.StartTime <= now
                    && !db.RoomRentalRevenueEntries.Any(e =>
                        e.RoomRentalRequestId == s.RoomRentalRequestId
                    )
                    && db.RoomRentalRequests.Any(r =>
                        r.RoomRentalRequestId == s.RoomRentalRequestId
                        && (
                            r.Status == RoomRentalRequestStatus.FullyPaid
                            || r.Status == RoomRentalRequestStatus.Completed
                        )
                    )
                )
                .OrderBy(s => s.ShowtimeId)
                .Select(s => new DueShowtime(
                    s.ShowtimeId,
                    s.RoomRentalRequestId!.Value,
                    s.RoomRentalRequest!.RequestCode,
                    s.StartTime
                ))
                .Take(BatchSize + failed.Count)
                .ToListAsync(cancellationToken);

            var batch = due.Where(d => !failed.Contains(d.RequestId)).ToList();
            if (batch.Count == 0)
                break;

            foreach (var item in batch)
            {
                try
                {
                    if (await RecognizeAsync(item, settings, cancellationToken))
                        total++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed.Add(item.RequestId);
                    logger.LogError(
                        ex,
                        "RoomRentalRevenueRecognitionFailed RequestId {RequestId}",
                        item.RequestId
                    );
                }
            }

            if (due.Count < BatchSize)
                break;
        }

        if (total > 0)
            logger.LogInformation("RoomRentalRevenueRecognized Count {Count}", total);
        return total;
    }

    private async Task<bool> RecognizeAsync(
        DueShowtime item,
        CinemaSettingsSnapshot settings,
        CancellationToken cancellationToken
    )
    {
        var businessDate = BusinessDay.Of(item.StartTime, settings.OpeningTime, settings.ClosingTime);
        var amount = await NetPaidAmountAsync(item.RequestId, cancellationToken);
        if (amount <= 0m)
            return false;

        var recorded = false;
        await db.InTransactionAsync(
            async () =>
            {
                if (
                    await db.RoomRentalRevenueEntries.AnyAsync(
                        e => e.RoomRentalRequestId == item.RequestId,
                        cancellationToken
                    )
                )
                    return;

                var now = DateTime.UtcNow;
                var entry = new RoomRentalRevenueEntry
                {
                    RoomRentalRequestId = item.RequestId,
                    ShowtimeId = item.ShowtimeId,
                    BusinessDate = businessDate,
                    Amount = amount,
                    OccurredAt = now,
                };
                db.RoomRentalRevenueEntries.Add(entry);
                await db.SaveChangesAsync(cancellationToken);

                await adjustmentWriter.RecordIfOriginalDayClosedAsync(
                    new LedgerAdjustmentInput(
                        businessDate,
                        LedgerAdjustmentType.RevenueRecognition,
                        string.Format(Messages.RoomRental_RevenueLedgerReason, item.RequestCode),
                        TicketRevenueDelta: amount,
                        SourceKey: $"room-rental-revenue:{item.RequestId}"
                    ),
                    cancellationToken
                );

                auditLogger.Record(
                    AuditActions.RecognizeRoomRentalRevenue,
                    AuditEntityTypes.RoomRentalRequest,
                    item.RequestId.ToString(),
                    null,
                    new
                    {
                        item.ShowtimeId,
                        businessDate,
                        amount,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
                recorded = true;
            },
            cancellationToken
        );

        if (!recorded)
            return false;

        await salesStats.RefreshShowtimesAsync([item.ShowtimeId], cancellationToken);
        return true;
    }

    private async Task<decimal> NetPaidAmountAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var orderIds = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.RoomRentalRequestId == requestId
                && o.Type == OrderType.RoomRental
                && o.Status == OrderStatus.Confirmed
            )
            .Select(o => o.OrderId)
            .ToListAsync(cancellationToken);

        var total = 0m;
        foreach (var orderId in orderIds)
            total += await ledger.GetNetPaidAmountAsync(orderId, cancellationToken);
        return total;
    }
}
