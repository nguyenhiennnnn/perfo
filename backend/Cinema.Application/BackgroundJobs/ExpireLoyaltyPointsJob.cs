using Cinema.Application.Services.Orders;

namespace Cinema.Application.BackgroundJobs;

public class ExpireLoyaltyPointsJob(LoyaltyExpiryService service)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.RecalculateAllAsync(cancellationToken);
}
