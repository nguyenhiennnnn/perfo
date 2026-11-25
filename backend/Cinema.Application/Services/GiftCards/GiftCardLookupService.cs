using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardLookupService(IAppDbContext db, IGiftCardAttemptGuard guard)
    : IGiftCardLookupService
{
    public async Task<GiftCardLookupDto> CheckBalanceAsync(
        int userId,
        string? code,
        CancellationToken cancellationToken
    )
    {
        await guard.EnsureNotLockedAsync(cancellationToken);

        var normalized = GiftCardCodes.Normalize(code);
        var card = normalized is null
            ? null
            : await db
                .GiftCards.AsNoTracking()
                .Where(c => c.Code == normalized)
                .Select(c => new
                {
                    c.Code,
                    c.Type,
                    c.Status,
                    c.PurchaserUserId,
                    c.Balance,
                    c.ReservedAmount,
                    c.ExpiresAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (card is null || (card.Type == GiftCardType.ExchangeCredit && card.PurchaserUserId != userId))
        {
            await guard.RecordFailureAsync(cancellationToken);
            throw new NotFoundException(Messages.NotFound_GiftCard);
        }

        return new GiftCardLookupDto(
            GiftCardCodes.Mask(card.Code),
            card.Type.ToString(),
            card.Status.ToString(),
            card.Balance,
            Math.Max(0m, card.Balance - card.ReservedAmount),
            card.ExpiresAt
        );
    }
}
