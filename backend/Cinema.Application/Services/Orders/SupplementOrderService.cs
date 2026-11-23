using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class SupplementOrderService(
    IAppDbContext db,
    IPricingService pricingService,
    IEnumerable<IOrderHoldPolicy> holdPolicies,
    IOrderResourceHoldService resourceHolds,
    IAuditLogger auditLogger,
    IAppCache cache,
    IOptions<BookingOptions> bookingOptions,
    ILogger<SupplementOrderService> logger
) : ISupplementOrderService
{
    public async Task<CounterOrderResponse> CreateAsync(
        int employeeId,
        int employeeUserId,
        CounterSupplementOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var originalOrderId =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.OrderLineId == request.OrderLineId)
                .Select(l => (int?)l.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_OrderLine);

        var holdSettings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var ticketTypes = (await cache.GetAsync(CacheKeys.TicketTypes, cancellationToken))
            .ToDictionary(t => t.TicketTypeId);
        var policy = holdPolicies.First(p => p.Channel == SalesChannel.Counter);
        var orderCode = await OrderCodes.GenerateUniqueAsync(
            db,
            bookingOptions.Value.BookingCodeMaxAttempts,
            cancellationToken
        );

        Order? created = null;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(originalOrderId, cancellationToken);

                var line =
                    await db
                        .OrderLines.AsNoTracking()
                        .Include(l => l.Order)
                        .Include(l => l.TicketType)
                        .Include(l => l.ShowtimeSeat)
                        .ThenInclude(ss => ss!.Seat)
                        .FirstOrDefaultAsync(
                            l => l.OrderLineId == request.OrderLineId,
                            cancellationToken
                        ) ?? throw new NotFoundException(Messages.NotFound_OrderLine);

                if (line.LineType != OrderLineType.Ticket || !line.RequiresCardPresentation)
                    throw new BusinessRuleException(Messages.Rule_CardCheckNotRequired);
                if (line.CardCheckStatus != CardCheckStatus.NotPresented)
                    throw new BusinessRuleException(Messages.Rule_SupplementRequiresNotPresented);
                if (
                    line.Order.Status != OrderStatus.Confirmed
                    || line.RefundedQuantity >= line.Quantity
                    || line.ShowtimeId is null
                    || line.ShowtimeSeat is null
                )
                    throw new BusinessRuleException(Messages.Rule_CardCheckOrderInvalid);

                var now = DateTime.UtcNow;
                var duplicate = await db.Orders.AnyAsync(
                    o =>
                        o.OriginalOrderId == originalOrderId
                        && o.Type == OrderType.Supplement
                        && (
                            o.Status == OrderStatus.Confirmed
                            || (o.Status == OrderStatus.Pending && o.ExpiresAt > now)
                        )
                        && o.Lines.Any(l => l.SupplementedLineId == request.OrderLineId),
                    cancellationToken
                );
                if (duplicate)
                    throw new ConflictAppException(Messages.Conflict_SupplementAlreadyExists);

                await policy.EnsureNoConflictingPendingAsync(
                    new OrderHoldPolicyContext(null, employeeUserId, employeeId),
                    cancellationToken
                );

                TicketTypeSnapshot? replacement = null;
                if (request.ReplacementTicketTypeId is { } replacementId)
                {
                    replacement =
                        ticketTypes.GetValueOrDefault(replacementId)
                        ?? throw new NotFoundException(Messages.NotFound_TicketType);
                    if (replacement.RequiresCardVerification)
                        throw new BusinessRuleException(
                            Messages.Rule_SupplementTicketTypeRequiresCard
                        );
                }

                var quote = await pricingService.QuoteAsync(
                    line.ShowtimeId.Value,
                    line.ShowtimeSeat.SeatId,
                    request.ReplacementTicketTypeId,
                    line.Order.CreatedAt,
                    cancellationToken
                );
                if (!quote.IsEligible)
                    throw new BusinessRuleException(
                        string.Format(
                            Messages.Rule_SupplementTicketTypeIneligible,
                            quote.IneligibleReason
                        )
                    );

                var difference = quote.ListPrice - line.PaidAmount;
                if (difference <= 0m)
                    throw new BusinessRuleException(Messages.Rule_SupplementNoDifference);

                var seat = line.ShowtimeSeat.Seat;
                var standardName = Messages.Common_TicketTypeStandard;
                var fee = new OrderLine
                {
                    LineType = OrderLineType.Fee,
                    Description = string.Format(
                        Messages.Order_SupplementFeeDescription,
                        $"{seat.SeatRow}{seat.SeatNumber}",
                        line.TicketType?.Name ?? standardName,
                        replacement?.Name ?? standardName
                    ),
                    SupplementedLineId = line.OrderLineId,
                    Quantity = 1,
                    UnitListPrice = difference,
                    LockedUnitPrice = difference,
                    PaidAmount = difference,
                };

                var order = new Order
                {
                    OrderCode = orderCode,
                    Type = OrderType.Supplement,
                    Channel = SalesChannel.Counter,
                    UserId = line.Order.UserId,
                    CustomerName = line.Order.CustomerName,
                    CustomerPhone = line.Order.CustomerPhone,
                    Status = OrderStatus.Pending,
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(holdSettings.HoldDurationMinutes),
                    CreatedByEmployeeId = employeeId,
                    OriginalOrderId = originalOrderId,
                };
                order.Lines.Add(fee);
                OrderPricing.ApplyTotals(order);

                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);

                await resourceHolds.ReserveAllAsync(order, cancellationToken);

                auditLogger.Record(
                    AuditActions.Create,
                    AuditEntityTypes.Order,
                    order.OrderId.ToString(),
                    null,
                    new
                    {
                        order.OrderCode,
                        Type = nameof(OrderType.Supplement),
                        OriginalOrderId = originalOrderId,
                        SupplementedLineId = line.OrderLineId,
                        ReplacementTicketTypeId = request.ReplacementTicketTypeId,
                        PreviousPaidAmount = line.PaidAmount,
                        ReplacementListPrice = quote.ListPrice,
                        Amount = difference,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);

                created = order;
            },
            cancellationToken
        );

        logger.LogInformation(
            "SupplementOrderCreated OrderId {OrderId} OriginalOrderId {OriginalOrderId} OrderLineId {OrderLineId}",
            created!.OrderId,
            originalOrderId,
            request.OrderLineId
        );
        return CounterOrderResponse.From(created);
    }
}
