using Cinema.Application.Caching;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.GiftCards;

public class GiftCardPurchaseService(
    IAppDbContext db,
    IAppCache cache,
    IOptions<BookingOptions> bookingOptions,
    ILogger<GiftCardPurchaseService> logger
) : IGiftCardPurchaseService
{
    public async Task<HoldOrderResponse> PurchaseAsync(
        int userId,
        PurchaseGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var amount = GiftCardRules.EnsureAmountAllowed(settings, request.Amount);
        var recipientEmail = GiftCardRules.NormalizeText(request.RecipientEmail, FieldLengths.Email);
        var recipientName = GiftCardRules.NormalizeText(
            request.RecipientName,
            FieldLengths.PersonName
        );
        if (recipientEmail is null || recipientName is null)
            throw new BusinessRuleException(Messages.Rule_GiftCardRecipientRequired);

        var now = DateTime.UtcNow;
        if (
            request.SendAt is { } sendAt
            && (sendAt <= now || sendAt >= now.AddMonths(settings.GiftCardValidityMonths))
        )
            throw new BusinessRuleException(Messages.Rule_GiftCardSendAtInvalid);

        var customerName =
            await db
                .Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_User);

        var orderCode = await OrderCodes.GenerateUniqueAsync(
            db,
            bookingOptions.Value.BookingCodeMaxAttempts,
            cancellationToken
        );

        var order = new Order
        {
            OrderCode = orderCode,
            Type = OrderType.GiftCard,
            Channel = SalesChannel.Online,
            UserId = userId,
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(settings.HoldDurationMinutes),
        };
        order.Lines.Add(
            GiftCardOrderLines.Create(
                amount,
                recipientEmail,
                recipientName,
                request.Message,
                request.SendAt
            )
        );
        OrderPricing.ApplyTotals(order);

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "GiftCardOrderCreated OrderId {OrderId} Amount {Amount}",
            order.OrderId,
            amount
        );
        return new HoldOrderResponse(
            order.OrderId,
            order.OrderCode,
            order.Status.ToDtoStatus(),
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.ExpiresAt!.Value
        );
    }
}
