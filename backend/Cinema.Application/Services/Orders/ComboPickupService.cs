using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class ComboPickupService(IAppDbContext db, ILogger<ComboPickupService> logger)
    : IComboPickupService
{
    private sealed record PickupLine(
        int OrderLineId,
        int Quantity,
        int RefundedQuantity,
        int QuantityFulfilled
    )
    {
        public int Remaining =>
            ComboPickupRules.Remaining(Quantity, RefundedQuantity, QuantityFulfilled);
    }

    public async Task<ComboPickupOrderDto> LookupAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        var orderId = await ResolveOrderIdAsync(code, cancellationToken);
        return await LoadOrderAsync(orderId, cancellationToken);
    }

    public async Task<ComboPickupResponse> PickUpAsync(
        int staffUserId,
        ComboPickupRequest request,
        CancellationToken cancellationToken
    )
    {
        var orderId = await ResolveOrderIdAsync(request.Code, cancellationToken);
        var now = DateTime.UtcNow;
        var pickedUpQuantity = 0;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var status =
                    await db
                        .Orders.AsNoTracking()
                        .Where(o => o.OrderId == orderId)
                        .Select(o => (OrderStatus?)o.Status)
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Order);
                EnsureOrderPickable(status);

                var lines = await db
                    .OrderLines.AsNoTracking()
                    .Where(l => l.OrderId == orderId && l.LineType == OrderLineType.Combo)
                    .Select(l => new PickupLine(
                        l.OrderLineId,
                        l.Quantity,
                        l.RefundedQuantity,
                        l.QuantityFulfilled
                    ))
                    .ToListAsync(cancellationToken);
                if (lines.Count == 0)
                    throw new BusinessRuleException(Messages.Rule_ComboPickupNoCombo);

                var plan = BuildPlan(lines, request.Items);
                foreach (var (lineId, quantity) in plan)
                {
                    var updated = await db
                        .OrderLines.Where(l =>
                            l.OrderLineId == lineId
                            && l.QuantityFulfilled + quantity <= l.Quantity - l.RefundedQuantity
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(
                                    l => l.QuantityFulfilled,
                                    l => l.QuantityFulfilled + quantity
                                ),
                            cancellationToken
                        );
                    if (updated == 0)
                        throw new ConflictAppException(
                            Messages.Conflict_ComboPickupExceedsRemaining
                        );

                    db.ComboPickups.Add(
                        new ComboPickup
                        {
                            OrderId = orderId,
                            OrderLineId = lineId,
                            Quantity = quantity,
                            PickedUpByUserId = staffUserId,
                            PickedUpAt = now,
                        }
                    );
                    pickedUpQuantity += quantity;
                }

                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "ComboPickedUp OrderId {OrderId} Quantity {Quantity} StaffUserId {StaffUserId}",
            orderId,
            pickedUpQuantity,
            staffUserId
        );
        return new ComboPickupResponse(
            await LoadOrderAsync(orderId, cancellationToken),
            pickedUpQuantity,
            now
        );
    }

    private static List<(int OrderLineId, int Quantity)> BuildPlan(
        List<PickupLine> lines,
        List<ComboPickupItemRequest>? items
    )
    {
        if (items is null || items.Count == 0)
        {
            var all = lines
                .Where(l => l.Remaining > 0)
                .OrderBy(l => l.OrderLineId)
                .Select(l => (l.OrderLineId, l.Remaining))
                .ToList();
            if (all.Count == 0)
                throw new BusinessRuleException(Messages.Rule_ComboPickupNothingToPickUp);
            return all;
        }

        var lineById = lines.ToDictionary(l => l.OrderLineId);
        var plan = new List<(int OrderLineId, int Quantity)>();
        foreach (var group in items.GroupBy(i => i.OrderLineId).OrderBy(g => g.Key))
        {
            var line =
                lineById.GetValueOrDefault(group.Key)
                ?? throw new NotFoundException(Messages.NotFound_OrderLine);
            var quantity = group.Sum(i => i.Quantity);
            if (quantity <= 0)
                throw new ValidationAppException(
                    Messages.Validation_ComboPickup_QuantityPositive,
                    new Dictionary<string, string[]>
                    {
                        ["items"] = [Messages.Validation_ComboPickup_QuantityPositive],
                    }
                );
            if (quantity > line.Remaining)
                throw new BusinessRuleException(
                    string.Format(Messages.Rule_ComboPickupQuantityExceeds, line.Remaining)
                );
            plan.Add((line.OrderLineId, quantity));
        }

        return plan;
    }

    private static void EnsureOrderPickable(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.Confirmed:
                return;
            case OrderStatus.Cancelled:
                throw new BusinessRuleException(Messages.Rule_ComboPickupOrderCancelled);
            case OrderStatus.Expired:
                throw new BusinessRuleException(Messages.Rule_ComboPickupOrderExpired);
            default:
                throw new BusinessRuleException(Messages.Rule_ComboPickupOrderNotPaid);
        }
    }

    private async Task<int> ResolveOrderIdAsync(string code, CancellationToken cancellationToken)
    {
        var raw = code.Trim();
        var normalized = raw.ToUpperInvariant();

        return await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderCode == normalized)
                .Select(o => (int?)o.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db
                .OrderLines.AsNoTracking()
                .Where(l =>
                    l.LineType == OrderLineType.Ticket
                    && (l.QrCode == raw || l.QrCode == normalized)
                )
                .Select(l => (int?)l.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ComboPickupCode);
    }

    private async Task<ComboPickupOrderDto> LoadOrderAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.Type,
                    o.Status,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == orderId && l.LineType == OrderLineType.Combo)
            .OrderBy(l => l.OrderLineId)
            .Select(l => new
            {
                l.OrderLineId,
                l.ComboId,
                ComboName = l.Combo!.Name,
                l.Quantity,
                l.RefundedQuantity,
                l.QuantityFulfilled,
            })
            .ToListAsync(cancellationToken);

        var history = await db
            .ComboPickups.AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.PickedUpAt)
            .ThenBy(p => p.ComboPickupId)
            .Select(p => new ComboPickupHistoryDto(
                p.ComboPickupId,
                p.OrderLineId,
                p.OrderLine.Combo!.Name,
                p.Quantity,
                p.PickedUpAt,
                p.PickedUpByUserId,
                p.PickedUpByUser.FullName
            ))
            .ToListAsync(cancellationToken);

        var lineDtos = lines
            .Select(l => new ComboPickupLineDto(
                l.OrderLineId,
                l.ComboId,
                l.ComboName,
                l.Quantity,
                l.RefundedQuantity,
                l.QuantityFulfilled,
                ComboPickupRules.Remaining(l.Quantity, l.RefundedQuantity, l.QuantityFulfilled)
            ))
            .ToList();
        var totalRemaining = lineDtos.Sum(l => l.RemainingQuantity);

        return new ComboPickupOrderDto(
            order.OrderId,
            order.OrderCode,
            order.Type.ToString(),
            order.Status.ToString(),
            order.CustomerName,
            order.CustomerPhone,
            order.CreatedAt,
            order.Type == OrderType.Combo,
            ComboPickupRules.IsUnclaimed(
                order.Type,
                order.Status,
                order.CreatedAt,
                totalRemaining,
                ComboPickupRules.UnclaimedCutoffUtc()
            ),
            lineDtos.Count > 0 && totalRemaining == 0,
            totalRemaining,
            lineDtos,
            history
        );
    }
}
