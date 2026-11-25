namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardExpiryService
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken);
}
