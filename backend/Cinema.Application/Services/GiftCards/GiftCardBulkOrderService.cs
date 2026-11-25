using Cinema.Application.Caching;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Import;
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

public class GiftCardBulkOrderService(
    IAppDbContext db,
    IAppCache cache,
    IImportSessionStore sessions,
    IOptions<BookingOptions> bookingOptions,
    ILogger<GiftCardBulkOrderService> logger
) : IGiftCardBulkOrderService
{
    public async Task<HoldOrderResponse> CreateOrderAsync(
        int userId,
        ImportApplyRequest request,
        CancellationToken cancellationToken
    )
    {
        var session = sessions.Get<GiftCardBulkImportRow>(request.PreviewId, userId);

        var invalidCount = session.Rows.Count(r => !r.IsValid);
        if (invalidCount > 0 && !request.SkipInvalidRows)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_ImportHasInvalidRows, invalidCount)
            );

        var validRows = session.Rows.Where(r => r.IsValid).ToList();
        if (validRows.Count == 0)
            throw new BusinessRuleException(Messages.Rule_ImportNoValidRows);

        if (!session.TryBeginApply())
            throw new ConflictAppException(Messages.Conflict_ImportApplyInProgress);

        try
        {
            var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
            var customerName =
                await db
                    .Users.AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_User);

            var now = DateTime.UtcNow;
            var order = new Order
            {
                OrderCode = await OrderCodes.GenerateUniqueAsync(
                    db,
                    bookingOptions.Value.BookingCodeMaxAttempts,
                    cancellationToken
                ),
                Type = OrderType.GiftCard,
                Channel = SalesChannel.Online,
                UserId = userId,
                CustomerName = customerName,
                Status = OrderStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(settings.HoldDurationMinutes),
            };

            foreach (var row in validRows)
            {
                var amount = GiftCardRules.EnsureAmountAllowed(settings, row.Data.FaceValue ?? 0m);
                order.Lines.Add(
                    GiftCardOrderLines.Create(
                        amount,
                        row.Data.RecipientEmail!,
                        row.Data.RecipientName!,
                        row.Data.Message,
                        null
                    )
                );
            }

            OrderPricing.ApplyTotals(order);
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            sessions.Remove(session.PreviewId);

            logger.LogInformation(
                "GiftCardBulkOrderCreated OrderId {OrderId} Cards {Count} Total {Total}",
                order.OrderId,
                validRows.Count,
                order.FinalAmount
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
        finally
        {
            session.EndApply();
        }
    }
}
