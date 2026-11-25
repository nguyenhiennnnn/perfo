using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardPurchasedQueryService(IAppDbContext db) : IGiftCardPurchasedQueryService
{
    public async Task<CursorPageResult<PurchasedGiftCardDto>> GetPurchasedAsync(
        int userId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .GiftCards.AsNoTracking()
            .Where(c => c.PurchaserUserId == userId && c.Type == GiftCardType.Gift);

        var cursorId = CursorCodec.Decode(cursor);
        if (cursorId is not null)
            query = query.Where(c => c.GiftCardId < cursorId);

        var rows = await query
            .OrderByDescending(c => c.GiftCardId)
            .Take(limit + 1)
            .Select(c => new
            {
                c.GiftCardId,
                c.Code,
                c.Status,
                c.FaceValue,
                c.Balance,
                c.RecipientName,
                c.RecipientEmail,
                c.Message,
                c.SendAt,
                c.ExpiresAt,
                c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var items = rows.Take(limit)
            .Select(c => new PurchasedGiftCardDto(
                c.GiftCardId,
                GiftCardCodes.Mask(c.Code),
                c.Status.ToString(),
                c.FaceValue,
                c.Balance,
                c.RecipientName,
                c.RecipientEmail,
                c.Message,
                c.SendAt,
                c.ExpiresAt,
                c.CreatedAt
            ))
            .ToList();

        return new CursorPageResult<PurchasedGiftCardDto>
        {
            Items = items,
            NextCursor = hasMore ? CursorCodec.Encode(items[^1].GiftCardId) : null,
            HasMore = hasMore,
        };
    }
}
