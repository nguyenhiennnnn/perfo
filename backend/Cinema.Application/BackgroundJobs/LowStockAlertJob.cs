using Cinema.Application.Interfaces;

namespace Cinema.Application.BackgroundJobs;

public class LowStockAlertJob(ILowStockAlertService service)
{
    public Task RunAsync(CancellationToken cancellationToken) => service.RunAsync(cancellationToken);
}
