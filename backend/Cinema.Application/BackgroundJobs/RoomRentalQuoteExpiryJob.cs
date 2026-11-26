using Cinema.Application.Interfaces.RoomRentals;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class RoomRentalQuoteExpiryJob(IRoomRentalQuoteExpiryService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.ExpireDueAsync(cancellationToken);
}
