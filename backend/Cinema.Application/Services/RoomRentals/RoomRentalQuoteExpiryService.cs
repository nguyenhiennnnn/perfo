using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalQuoteExpiryService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ILogger<RoomRentalQuoteExpiryService> logger
) : IRoomRentalQuoteExpiryService
{
    private const int BatchSize = 100;

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        while (true)
        {
            var now = DateTime.UtcNow;
            var due = await DueRequests(now)
                .AsNoTracking()
                .OrderBy(r => r.RoomRentalRequestId)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            var expired = 0;
            foreach (var request in due)
            {
                try
                {
                    if (await ExpireAsync(request, now, cancellationToken))
                        expired++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "RoomRentalQuoteExpiryFailed RequestId {RequestId}",
                        request.RoomRentalRequestId
                    );
                }
            }

            total += expired;
            if (expired == 0 || due.Count < BatchSize)
                break;
        }

        if (total > 0)
            logger.LogInformation("RoomRentalQuotesExpired Count {Count}", total);
        return total;
    }

    private async Task<bool> ExpireAsync(
        RoomRentalRequest request,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var claimed = false;
        var before = AuditSnapshots.RoomRentalRequestState(request);
        await db.InTransactionAsync(
            async () =>
            {
                var updated = await DueRequests(now)
                    .Where(r => r.RoomRentalRequestId == request.RoomRentalRequestId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(r => r.Status, RoomRentalRequestStatus.QuoteExpired)
                                .SetProperty(r => r.UpdatedAt, now),
                        cancellationToken
                    );
                if (updated == 0)
                    return;

                auditLogger.Record(
                    AuditActions.ExpireRoomRentalQuote,
                    AuditEntityTypes.RoomRentalRequest,
                    request.RoomRentalRequestId.ToString(),
                    before,
                    AuditSnapshots.RoomRentalRequestState(
                        RoomRentalRequestStatus.QuoteExpired,
                        request.StatusNote,
                        now
                    )
                );
                await db.SaveChangesAsync(cancellationToken);
                claimed = true;
            },
            cancellationToken
        );
        return claimed;
    }

    private IQueryable<RoomRentalRequest> DueRequests(DateTime now) =>
        db.RoomRentalRequests.Where(r =>
            r.Status == RoomRentalRequestStatus.Quoted
            && db.RoomRentalQuotes.Any(q =>
                q.RoomRentalRequestId == r.RoomRentalRequestId
                && q.SentAt != null
                && q.ValidUntil <= now
                && !db.RoomRentalQuotes.Any(n =>
                    n.RoomRentalRequestId == r.RoomRentalRequestId
                    && n.SentAt != null
                    && n.VersionNumber > q.VersionNumber
                )
            )
        );
}
