using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Payments;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderPromotionService(
    IAppDbContext db,
    IOrderPriceCalculator priceCalculator,
    PromotionResourceHold promotionHold,
    ILogger<OrderPromotionService> logger
) : IOrderPromotionService
{
    public Task<OrderPromotionResponse> ApplyAsync(
        int orderId,
        OrderPromotionCaller caller,
        string code,
        CancellationToken cancellationToken
    ) => RecalculateAsync(orderId, caller, code, cancellationToken);

    public Task<OrderPromotionResponse> RemoveAsync(
        int orderId,
        OrderPromotionCaller caller,
        CancellationToken cancellationToken
    ) => RecalculateAsync(orderId, caller, null, cancellationToken);

    private async Task<OrderPromotionResponse> RecalculateAsync(
        int orderId,
        OrderPromotionCaller caller,
        string? code,
        CancellationToken cancellationToken
    )
    {
        OrderPromotionResponse? response = null;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var order =
                    await db
                        .Orders.Include(o => o.Lines)
                        .Include(o => o.PriceAdjustments)
                        .ThenInclude(a => a.Allocations)
                        .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Order);

                EnsureAccess(order, caller);
                if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
                    throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);

                await promotionHold.ReleaseAsync(orderId, cancellationToken);

                var result = await priceCalculator.ApplyAsync(
                    order,
                    new OrderPricingOptions(code),
                    cancellationToken
                );
                if (result.CodeResult is { IsValid: false } rejected)
                    throw PromotionRejections.ToException(rejected);

                await promotionHold.ReserveAsync(new ResourceHoldRequest(order), cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                var applied = result.AppliedPromotions.FirstOrDefault();
                response = new OrderPromotionResponse(
                    order.OrderId,
                    order.OrderCode,
                    order.TotalAmount,
                    order.DiscountAmount,
                    order.FinalAmount,
                    order.ExpiresAt!.Value,
                    applied?.Promotion.Code,
                    applied?.Promotion.Name,
                    order
                        .PriceAdjustments.Where(a => a.Type == PriceAdjustmentType.Promotion)
                        .Sum(a => a.Amount)
                );
            },
            cancellationToken
        );

        var final = response!;
        logger.LogInformation(
            "OrderPromotionRecalculated OrderId {OrderId} PromotionCode {PromotionCode} FinalAmount {FinalAmount}",
            final.OrderId,
            final.PromotionCode,
            final.FinalAmount
        );
        return final;
    }

    private static void EnsureAccess(Order order, OrderPromotionCaller caller)
    {
        if (caller.CustomerUserId is { } userId)
        {
            if (order.UserId != userId)
                throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
            return;
        }

        if (order.Channel != SalesChannel.Counter)
            throw new BusinessRuleException(Messages.Rule_PromotionCounterOrderOnly);
    }
}
