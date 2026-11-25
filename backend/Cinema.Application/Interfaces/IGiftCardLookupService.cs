using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces;

public interface IGiftCardLookupService
{
    Task<GiftCardLookupDto> CheckBalanceAsync(
        int userId,
        string? code,
        CancellationToken cancellationToken
    );
}
