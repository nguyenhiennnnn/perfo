using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardAdminQueryService(IAppDbContext db) : IGiftCardAdminQueryService
{
    public async Task<OffsetPageResult<AdminGiftCardListItemDto>> GetAsync(
        AdminGiftCardListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.GiftCards.AsNoTracking();

        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<GiftCardStatus>(filter.Status.Trim(), true, out var status)
        )
            query = query.Where(c => c.Status == status);

        if (
            !string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<GiftCardType>(filter.Type.Trim(), true, out var type)
        )
            query = query.Where(c => c.Type == type);

        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var code = GiftCardCodes.Normalize(search);
            if (code is not null)
                query = query.Where(c => c.Code == code);
            else
                query = query.ApplySearch(
                    search,
                    (q, pattern) =>
                        q.Where(c =>
                            (c.RecipientEmail != null && EF.Functions.ILike(c.RecipientEmail, pattern))
                            || (c.RecipientName != null && EF.Functions.ILike(c.RecipientName, pattern))
                            || EF.Functions.ILike(c.PurchaserUser.Email, pattern)
                            || EF.Functions.ILike(c.PurchaserUser.FullName, pattern)
                        )
                );
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(c => c.GiftCardId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.GiftCardId,
                c.Code,
                c.Type,
                c.Status,
                c.FaceValue,
                c.Balance,
                c.ReservedAmount,
                c.PurchaserUserId,
                PurchaserName = c.PurchaserUser.FullName,
                PurchaserEmail = c.PurchaserUser.Email,
                c.RecipientName,
                c.RecipientEmail,
                c.SendAt,
                c.ExpiresAt,
                c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(c => new AdminGiftCardListItemDto(
                c.GiftCardId,
                GiftCardCodes.Mask(c.Code),
                c.Type.ToString(),
                c.Status.ToString(),
                c.FaceValue,
                c.Balance,
                c.ReservedAmount,
                c.PurchaserUserId,
                c.PurchaserName,
                c.PurchaserEmail,
                c.RecipientName,
                c.RecipientEmail,
                c.SendAt,
                c.ExpiresAt,
                c.CreatedAt
            ))
            .ToList();

        return OffsetPageResult<AdminGiftCardListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<List<AdminGiftCardLedgerEntryDto>> GetHistoryAsync(
        int giftCardId,
        CancellationToken cancellationToken
    )
    {
        if (!await db.GiftCards.AsNoTracking().AnyAsync(c => c.GiftCardId == giftCardId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_GiftCard);

        var rows = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l => l.GiftCardId == giftCardId)
            .OrderByDescending(l => l.OccurredAt)
            .ThenByDescending(l => l.GiftCardLedgerId)
            .Select(l => new
            {
                l.GiftCardLedgerId,
                l.EntryType,
                l.BalanceChange,
                l.ReservedChange,
                l.BalanceAfter,
                l.ReservedAfter,
                l.OrderId,
                l.OccurredAt,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(l => new AdminGiftCardLedgerEntryDto(
                l.GiftCardLedgerId,
                l.EntryType.ToString(),
                l.BalanceChange,
                l.ReservedChange,
                l.BalanceAfter,
                l.ReservedAfter,
                l.OrderId,
                l.OccurredAt
            ))
            .ToList();
    }
}
