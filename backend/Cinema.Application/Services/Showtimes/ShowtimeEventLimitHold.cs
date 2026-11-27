using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Showtimes;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEventLimitHold(IShowtimeEventLimitService limitService) : IResourceHold
{
    public string ResourceType => "ShowtimeEventLimit";

    public Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken) =>
        limitService.EnsureWithinLimitsAsync(request.Order, cancellationToken);

    public Task CommitAsync(int orderId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ReleaseAsync(int orderId, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
