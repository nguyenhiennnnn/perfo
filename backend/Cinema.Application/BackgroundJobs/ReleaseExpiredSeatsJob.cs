using Cinema.Application.Interfaces;

namespace Cinema.Application.BackgroundJobs;

public class ReleaseExpiredSeatsJob(
    ISeatExpirationService seatExpirationService,
    ISeatBlockService seatBlockService
)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await seatExpirationService.ExpireDueOrdersAsync(cancellationToken);
        await seatBlockService.ReleaseExpiredAsync(cancellationToken);
    }
}
