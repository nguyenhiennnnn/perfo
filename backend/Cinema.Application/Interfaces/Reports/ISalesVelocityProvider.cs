namespace Cinema.Application.Interfaces.Reports;

public interface ISalesVelocityProvider
{
    Task<decimal?> GetTicketsPerHourAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    );
}
