using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Memberships;

public class MembershipPurchaseService(
    IAppDbContext db,
    IAppCache cache,
    IOptions<BookingOptions> bookingOptions,
    ILogger<MembershipPurchaseService> logger
) : IMembershipPurchaseService
{
    public async Task<HoldOrderResponse> PurchaseAsync(
        int userId,
        int planId,
        CancellationToken cancellationToken
    )
    {
        var plan =
            await db
                .MembershipPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembershipPlanId == planId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_MembershipPlan);
        if (!plan.IsActive)
            throw new BusinessRuleException(Messages.Rule_MembershipPlanNotAvailable);

        var customerName =
            await db
                .Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_User);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var order = new Order
        {
            OrderCode = await OrderCodes.GenerateUniqueAsync(
                db,
                bookingOptions.Value.BookingCodeMaxAttempts,
                cancellationToken
            ),
            Type = OrderType.Membership,
            Channel = SalesChannel.Online,
            UserId = userId,
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(settings.HoldDurationMinutes),
        };
        order.Lines.Add(
            new OrderLine
            {
                LineType = OrderLineType.Fee,
                Description = string.Format(Messages.Order_MembershipLineDescription, plan.Name),
                Quantity = 1,
                UnitListPrice = plan.Price,
                LockedUnitPrice = plan.Price,
                MembershipPlanId = plan.MembershipPlanId,
            }
        );
        OrderPricing.ApplyTotals(order);

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "MembershipOrderCreated OrderId {OrderId} MembershipPlanId {MembershipPlanId}",
            order.OrderId,
            plan.MembershipPlanId
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
