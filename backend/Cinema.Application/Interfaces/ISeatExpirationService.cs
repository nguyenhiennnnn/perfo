namespace Cinema.Application.Interfaces;

public interface ISeatExpirationService
{
    Task<int> ReleaseStaleHeldSeatsAsync(
        List<int>? showtimeIds,
        CancellationToken cancellationToken
    );

    Task ExpireDueOrdersAsync(CancellationToken cancellationToken);
}
