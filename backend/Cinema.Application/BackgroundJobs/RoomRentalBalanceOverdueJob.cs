using Cinema.Application.Interfaces.RoomRentals;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class RoomRentalBalanceOverdueJob(IRoomRentalBalanceOverdueService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.CancelOverdueAsync(cancellationToken);
}
