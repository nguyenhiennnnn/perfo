using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public interface ICardCheckService
{
    Task<RecordCardNotPresentedResponse> RecordNotPresentedAsync(
        int employeeUserId,
        RecordCardNotPresentedRequest request,
        CancellationToken cancellationToken
    );
}
