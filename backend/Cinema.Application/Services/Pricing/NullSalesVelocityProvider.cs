using Cinema.Application.Interfaces;

namespace Cinema.Application.Services.Pricing;

public sealed class NullSalesVelocityProvider : ISalesVelocityProvider
{
    public Task<decimal?> GetTicketsPerHourAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    ) => Task.FromResult<decimal?>(null);
}
