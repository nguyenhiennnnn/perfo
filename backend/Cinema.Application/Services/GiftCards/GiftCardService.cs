using Cinema.Application.Caching;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardService(IAppDbContext db, IAppCache cache) : IGiftCardService
{
    private const int CodeGenerationAttempts = 5;

    public async Task<GiftCard> IssueAsync(
        IssueGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var faceValue = Math.Round(request.FaceValue, 0, MidpointRounding.AwayFromZero);
        if (faceValue <= 0)
            throw new BusinessRuleException(Messages.Rule_GiftCardAmountInvalid);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var recipientEmail = GiftCardRules.NormalizeText(request.RecipientEmail, FieldLengths.Email);
        var recipientName = GiftCardRules.NormalizeText(request.RecipientName, FieldLengths.PersonName);

        if (request.Type == GiftCardType.Gift && (recipientEmail is null || recipientName is null))
            throw new BusinessRuleException(Messages.Rule_GiftCardRecipientRequired);

        var now = DateTime.UtcNow;
        var card = new GiftCard
        {
            Code = await GiftCardCodes.GenerateUniqueAsync(
                db,
                CodeGenerationAttempts,
                cancellationToken
            ),
            Type = request.Type,
            FaceValue = faceValue,
            Balance = faceValue,
            ReservedAmount = 0m,
            PurchaserUserId = request.PurchaserUserId,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Message = GiftCardRules.NormalizeText(request.Message, FieldLengths.GiftCardMessage),
            SendAt = request.SendAt,
            ExpiresAt = request.ExpiresAt ?? now.AddMonths(settings.GiftCardValidityMonths),
            Status =
                request.Type == GiftCardType.Gift ? GiftCardStatus.PendingSend : GiftCardStatus.Sent,
            CreatedAt = now,
        };
        card.LedgerEntries.Add(
            new GiftCardLedger
            {
                EntryType = GiftCardLedgerType.Issue,
                BalanceChange = faceValue,
                ReservedChange = 0m,
                BalanceAfter = faceValue,
                ReservedAfter = 0m,
                OrderId = request.OrderId,
                OccurredAt = now,
            }
        );
        db.GiftCards.Add(card);
        return card;
    }

    public async Task<GiftCard?> FindByCodeAsync(
        string? code,
        CancellationToken cancellationToken
    )
    {
        var normalized = GiftCardCodes.Normalize(code);
        if (normalized is null)
            return null;

        return await db.GiftCards.FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);
    }

    public async Task<GiftCardBalanceCheckDto> CheckBalanceAsync(
        int giftCardId,
        CancellationToken cancellationToken
    )
    {
        var card =
            await db
                .GiftCards.AsNoTracking()
                .Where(c => c.GiftCardId == giftCardId)
                .Select(c => new { c.Balance, c.ReservedAmount })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_GiftCard);

        var entries = db.GiftCardLedgers.AsNoTracking().Where(l => l.GiftCardId == giftCardId);
        var ledgerBalance = await entries.SumAsync(l => l.BalanceChange, cancellationToken);
        var ledgerReserved = await entries.SumAsync(l => l.ReservedChange, cancellationToken);

        return new GiftCardBalanceCheckDto(
            giftCardId,
            card.Balance,
            ledgerBalance,
            card.ReservedAmount,
            ledgerReserved
        );
    }
}
