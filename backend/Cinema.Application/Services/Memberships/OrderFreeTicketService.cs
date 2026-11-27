using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Services.Orders;
using Cinema.Application.Services.Payments;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Memberships;

public class OrderFreeTicketService(
    IAppDbContext db,
    IOrderPriceCalculator priceCalculator,
    PromotionResourceHold promotionHold,
    IMembershipFreeTicketHoldService ticketHolds,
    IMembershipFreeTicketQueryService balances,
    IGiftCardHoldService giftCardHolds,
    IPaymentLedgerService ledger,
    IOrderConfirmationService confirmationService,
    ILogger<OrderFreeTicketService> logger
) : IOrderFreeTicketService
{
    public async Task<OrderFreeTicketResponse> ApplyAsync(
        int orderId,
        int userId,
        int orderLineId,
        CancellationToken cancellationToken
    )
    {
        var coversOrder = false;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);
                var order = await LoadPendingOrderAsync(orderId, userId, cancellationToken);

                var line =
                    order.Lines.FirstOrDefault(l => l.OrderLineId == orderLineId)
                    ?? throw new NotFoundException(Messages.NotFound_OrderLine);
                if (
                    order.Type != OrderType.Ticket
                    || line.LineType != OrderLineType.Ticket
                    || line.ShowtimeId is not { } showtimeId
                )
                    throw new BusinessRuleException(Messages.Rule_FreeTicketLineNotEligible);
                if (FindAdjustment(order, line.OrderLineId) is not null)
                    throw new BusinessRuleException(Messages.Rule_FreeTicketAlreadyApplied);
                if (
                    order.PriceAdjustments.Any(a =>
                        a.Type == PriceAdjustmentType.PackagePrice
                        && a.Allocations.Any(x => x.OrderLineId == line.OrderLineId)
                    )
                )
                    throw new BusinessRuleException(Messages.Rule_FreeTicketLineNotEligible);

                var showtimeInfo = await db
                    .Showtimes.AsNoTracking()
                    .Where(s => s.ShowtimeId == showtimeId)
                    .Select(s => new
                    {
                        s.Kind,
                        AllowsFree = s.EventSetting != null && s.EventSetting.AllowMembershipFreeTickets,
                    })
                    .FirstAsync(cancellationToken);
                if (!MembershipFreeTicketRules.AllowsShowtime(showtimeInfo.Kind, showtimeInfo.AllowsFree))
                    throw new BusinessRuleException(Messages.Rule_FreeTicketNotForShowtime);

                var value = line.LockedUnitPrice * line.Quantity;
                if (value <= 0m)
                    throw new BusinessRuleException(Messages.Rule_FreeTicketLineNotEligible);

                var ticketId = await ticketHolds.ReserveAsync(
                    userId,
                    orderId,
                    line.OrderLineId,
                    showtimeId,
                    value,
                    cancellationToken
                );

                var lines = order.Lines.ToList();
                var shares = lines.Select(l => l.OrderLineId == line.OrderLineId ? value : 0m).ToArray();
                order.PriceAdjustments.Add(
                    OrderPricing.BuildAdjustment(
                        PriceAdjustmentType.FreeTicket,
                        nameof(MembershipFreeTicket),
                        ticketId,
                        lines,
                        shares
                    )
                );

                await RepriceAsync(order, cancellationToken);
                await FailPendingChargesAsync(orderId, cancellationToken);
                coversOrder = await IsFullyCoveredAsync(orderId, cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "OrderFreeTicketApplied OrderId {OrderId} OrderLineId {OrderLineId} CoversOrder {CoversOrder}",
            orderId,
            orderLineId,
            coversOrder
        );

        if (coversOrder)
        {
            var confirmation = await confirmationService.TryConfirmAsync(
                orderId,
                cancellationToken
            );
            if (confirmation == OrderConfirmationResult.NotPayable)
                throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);
        }

        return await BuildResponseAsync(orderId, userId, cancellationToken);
    }

    public async Task<OrderFreeTicketResponse> RemoveAsync(
        int orderId,
        int userId,
        int orderLineId,
        CancellationToken cancellationToken
    )
    {
        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);
                var order = await LoadPendingOrderAsync(orderId, userId, cancellationToken);

                var adjustment =
                    FindAdjustment(order, orderLineId)
                    ?? throw new BusinessRuleException(Messages.Rule_FreeTicketNotApplied);
                order.PriceAdjustments.Remove(adjustment);
                if (adjustment.AdjustmentId != 0)
                    db.OrderPriceAdjustments.Remove(adjustment);

                await ticketHolds.ReleaseLineAsync(orderId, orderLineId, cancellationToken);
                await RepriceAsync(order, cancellationToken);
                await FailPendingChargesAsync(orderId, cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "OrderFreeTicketRemoved OrderId {OrderId} OrderLineId {OrderLineId}",
            orderId,
            orderLineId
        );
        return await BuildResponseAsync(orderId, userId, cancellationToken);
    }

    private static OrderPriceAdjustment? FindAdjustment(Order order, int orderLineId) =>
        order.PriceAdjustments.FirstOrDefault(a =>
            a.Type == PriceAdjustmentType.FreeTicket
            && a.Allocations.Any(x => x.OrderLineId == orderLineId)
        );

    private async Task<Order> LoadPendingOrderAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.Include(o => o.Lines)
                .Include(o => o.PriceAdjustments)
                .ThenInclude(a => a.Allocations)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);

        return order;
    }

    private async Task RepriceAsync(Order order, CancellationToken cancellationToken)
    {
        var options = await ResolvePricingOptionsAsync(order, cancellationToken);

        await promotionHold.ReleaseAsync(order.OrderId, cancellationToken);
        var result = await priceCalculator.ApplyAsync(order, options, cancellationToken);
        if (result.CodeResult is { IsValid: false })
            await priceCalculator.ApplyAsync(
                order,
                new OrderPricingOptions(null, options.ApplyAutoPromotions),
                cancellationToken
            );

        await promotionHold.ReserveAsync(new ResourceHoldRequest(order), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await giftCardHolds.TrimAsync(
            order.OrderId,
            order.FinalAmount - await ledger.GetNetPaidAmountAsync(order.OrderId, cancellationToken),
            cancellationToken
        );
    }

    private async Task<OrderPricingOptions> ResolvePricingOptionsAsync(
        Order order,
        CancellationToken cancellationToken
    )
    {
        var promotionIds = order
            .PriceAdjustments.Where(a =>
                a.Type == PriceAdjustmentType.Promotion && a.ReferenceId != null
            )
            .Select(a => a.ReferenceId!.Value)
            .ToList();
        if (promotionIds.Count == 0)
            return OrderPricingOptions.NoPromotions;

        var promotions = await db
            .Promotions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => promotionIds.Contains(p.PromotionId))
            .Select(p => new { p.Code, p.Type })
            .ToListAsync(cancellationToken);
        return new OrderPricingOptions(
            promotions.FirstOrDefault(p => p.Type != PromotionType.AutoApply)?.Code,
            promotions.Any(p => p.Type == PromotionType.AutoApply)
        );
    }

    private async Task<bool> IsFullyCoveredAsync(int orderId, CancellationToken cancellationToken)
    {
        var summary = await ledger.GetSummaryAsync(orderId, cancellationToken);
        var reserved = await giftCardHolds.GetReservedAmountAsync(orderId, cancellationToken);
        return summary.OutstandingAmount - reserved <= 0m;
    }

    private Task<int> FailPendingChargesAsync(int orderId, CancellationToken cancellationToken) =>
        db
            .PaymentTransactions.Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && t.Status == PaymentStatus.Pending
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                cancellationToken
            );

    private async Task<OrderFreeTicketResponse> BuildResponseAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.Status,
                o.TotalAmount,
                o.DiscountAmount,
                o.FinalAmount,
            })
            .FirstAsync(cancellationToken);

        var tickets = await db
            .MembershipFreeTickets.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.OrderId == orderId && t.OrderLineId != null)
            .OrderBy(t => t.OrderLineId)
            .Select(t => new OrderFreeTicketItemDto(
                t.OrderLineId!.Value,
                t.MembershipFreeTicketId,
                t.ConvertedAmount ?? 0m
            ))
            .ToListAsync(cancellationToken);

        var payable = 0m;
        if (order.Status == OrderStatus.Pending)
        {
            var summary = await ledger.GetSummaryAsync(orderId, cancellationToken);
            var reserved = await giftCardHolds.GetReservedAmountAsync(orderId, cancellationToken);
            payable = Math.Max(0m, summary.OutstandingAmount - reserved);
        }

        var balance = await balances.GetMineAsync(userId, cancellationToken);
        return new OrderFreeTicketResponse(
            order.OrderId,
            order.OrderCode,
            order.Status.ToDtoStatus(),
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            tickets.Sum(t => t.ConvertedAmount),
            payable,
            payable <= 0m,
            balance.Remaining,
            tickets
        );
    }
}
