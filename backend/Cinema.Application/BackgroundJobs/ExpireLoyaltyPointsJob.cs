using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Services.Orders;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class ExpireLoyaltyPointsJob(LoyaltyExpiryService service, ILoyaltyPointService points)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await points.ExpireAsync(DateTime.UtcNow, cancellationToken);
        await service.RecalculateAllAsync(cancellationToken);
    }
}
