using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Events;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;


namespace Cinema.Application.Services.GiftCards;

public class GiftCardIssueHandler(IAppDbContext db, IGiftCardService giftCards)
    : IInTransactionEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken)
    {
        var lines = await db
            .OrderLines.Include(l => l.Order)
            .Where(l =>
                l.OrderId == businessEvent.OrderId
                && l.LineType == OrderLineType.GiftCard
                && l.GiftCardId == null
            )
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
        {
            line.GiftCard = await giftCards.IssueAsync(
                new IssueGiftCardRequest(
                    GiftCardType.Gift,
                    line.LockedUnitPrice,
                    line.Order.UserId!.Value,
                    line.GiftRecipientEmail,
                    line.GiftRecipientName,
                    line.GiftMessage,
                    line.GiftSendAt,
                    line.OrderId
                ),
                cancellationToken
            );
        }
    }
}
