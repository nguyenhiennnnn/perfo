namespace Cinema.Application.Interfaces;

public interface ISalesVelocityProvider
{
    Task<decimal?> GetTicketsPerHourAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    );
}
