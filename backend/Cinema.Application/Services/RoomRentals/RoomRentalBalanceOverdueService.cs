using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalBalanceOverdueService(
    IAppDbContext db,
    IRoomRentalCancellationService cancellationService,
    ILogger<RoomRentalBalanceOverdueService> logger
) : IRoomRentalBalanceOverdueService
{
    private const int BatchSize = 100;

    public async Task<int> CancelOverdueAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        var failed = new HashSet<int>();

        while (true)
        {
            var due = await RoomRentalCancellationRules
                .OverdueRequests(db, DateTime.UtcNow)
                .AsNoTracking()
                .OrderBy(r => r.RoomRentalRequestId)
                .Select(r => r.RoomRentalRequestId)
                .Take(BatchSize + failed.Count)
                .ToListAsync(cancellationToken);

            var batch = due.Where(id => !failed.Contains(id)).ToList();
            if (batch.Count == 0)
                break;

            foreach (var requestId in batch)
            {
                try
                {
                    if (
                        await cancellationService.TryCancelOverdueAsync(
                            requestId,
                            cancellationToken
                        )
                        is not null
                    )
                        total++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed.Add(requestId);
                    logger.LogError(
                        ex,
                        "RoomRentalBalanceOverdueFailed RequestId {RequestId}",
                        requestId
                    );
                }
            }

            if (due.Count < BatchSize + failed.Count)
                break;
        }

        if (total > 0)
            logger.LogInformation("RoomRentalBalanceOverdueCancelled Count {Count}", total);
        return total;
    }
}
