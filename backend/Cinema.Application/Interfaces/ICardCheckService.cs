using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface ICardCheckService
{
    Task<RecordCardNotPresentedResponse> RecordNotPresentedAsync(
        int employeeUserId,
        RecordCardNotPresentedRequest request,
        CancellationToken cancellationToken
    );
}
