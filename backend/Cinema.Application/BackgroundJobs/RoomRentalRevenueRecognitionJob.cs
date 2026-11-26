using Cinema.Application.Interfaces.RoomRentals;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class RoomRentalRevenueRecognitionJob(IRoomRentalRevenueService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.RecognizeDueAsync(cancellationToken);
}
