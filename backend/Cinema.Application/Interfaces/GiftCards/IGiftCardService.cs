using Cinema.Application.DTOs.GiftCards;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardService
{
    Task<GiftCard> IssueAsync(IssueGiftCardRequest request, CancellationToken cancellationToken);

    Task<GiftCard?> FindByCodeAsync(string? code, CancellationToken cancellationToken);

    Task<GiftCardBalanceCheckDto> CheckBalanceAsync(
        int giftCardId,
        CancellationToken cancellationToken
    );
}
