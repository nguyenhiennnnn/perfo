using Cinema.Application.Caching;
using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Ledger;

public class LedgerAdjustmentWriter(IAppDbContext db, IAppCache cache) : ILedgerAdjustmentWriter
{
    public async Task<bool> RecordIfOriginalDayClosedAsync(
        LedgerAdjustmentInput input,
        CancellationToken cancellationToken
    )
    {
        if (input.IsEmpty)
            return false;

        var closed = await db
            .DailyClosings.AsNoTracking()
            .AnyAsync(
                c => c.BusinessDate == input.OriginalDate && c.Status == DailyClosingStatus.Closed,
                cancellationToken
            );
        if (!closed)
            return false;

        if (
            input.SourceKey is { } sourceKey
            && await db.LedgerAdjustments.AsNoTracking()
                .AnyAsync(
                    a => a.SourceKey == sourceKey && a.OriginalDate == input.OriginalDate,
                    cancellationToken
                )
        )
            return false;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var postingDate = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        if (postingDate <= input.OriginalDate)
            return false;

        db.LedgerAdjustments.Add(input.ToEntity(postingDate, now));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (
                input.SourceKey is { } key
                && await db.LedgerAdjustments.AsNoTracking()
                    .AnyAsync(
                        a => a.SourceKey == key && a.OriginalDate == input.OriginalDate,
                        cancellationToken
                    )
            )
                return false;
            throw;
        }

        return true;
    }
}
