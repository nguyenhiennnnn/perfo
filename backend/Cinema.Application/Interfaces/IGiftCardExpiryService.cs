namespace Cinema.Application.Interfaces;

public interface IGiftCardExpiryService
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken);
}
