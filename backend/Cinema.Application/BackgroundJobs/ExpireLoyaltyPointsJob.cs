using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Services.Orders;

namespace Cinema.Application.BackgroundJobs;

public class ExpireLoyaltyPointsJob(LoyaltyExpiryService service, ILoyaltyPointService points)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await points.ExpireAsync(DateTime.UtcNow, cancellationToken);
        await service.RecalculateAllAsync(cancellationToken);
    }
}
