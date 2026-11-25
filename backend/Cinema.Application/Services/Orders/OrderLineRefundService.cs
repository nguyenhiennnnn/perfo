using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderLineRefundService(IAppDbContext db, IOrderRefundService orderRefunds)
    : IOrderLineRefundService
{
    public async Task<OrderLineRefundResult> RefundLinesAsync(
        OrderLineRefundCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.Percent <= 0m || command.Percent > 100m)
            throw new BusinessRuleException(Messages.Refund_PercentInvalid);

        var requests = command
            .Lines.GroupBy(l => l.OrderLineId)
            .Select(g => new OrderLineRefundItem(g.Key, g.Sum(x => x.Quantity)))
            .ToList();
        if (requests.Count == 0 || requests.Any(r => r.Quantity <= 0))
            throw new BusinessRuleException(Messages.Refund_LineQuantityInvalid);

        var lineResults = new List<OrderLineRefundLineResult>(requests.Count);
        IReadOnlyList<OrderRefundReservation> reservations = [];
        var total = 0m;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(command.OrderId, cancellationToken);

                var order =
                    await db
                        .Orders.AsNoTracking()
                        .FirstOrDefaultAsync(o => o.OrderId == command.OrderId, cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Order);

                var lineIds = requests.Select(r => r.OrderLineId).ToList();
                var lines = await db
                    .OrderLines.AsNoTracking()
                    .Where(l => l.OrderId == order.OrderId && lineIds.Contains(l.OrderLineId))
                    .ToDictionaryAsync(l => l.OrderLineId, cancellationToken);

                var exchangedLineIds = await db
                    .OrderExchanges.AsNoTracking()
                    .Where(e => lineIds.Contains(e.OldOrderLineId))
                    .Select(e => e.OldOrderLineId)
                    .ToListAsync(cancellationToken);

                var updates = new List<(OrderLine Line, int Units, decimal Amount)>();
                foreach (var request in requests)
                {
                    if (!lines.TryGetValue(request.OrderLineId, out var line))
                        throw new NotFoundException(Messages.NotFound_OrderLine);
                    if (exchangedLineIds.Contains(line.OrderLineId))
                        throw new BusinessRuleException(Messages.Refund_LineExchanged);
                    if (request.Quantity > OrderRefundCalculator.RefundableUnits(line))
                        throw new BusinessRuleException(Messages.Refund_LineQuantityExceeds);

                    var amount = OrderRefundCalculator.LineRefundAmount(
                        line.PaidAmount,
                        line.Quantity,
                        line.RefundedQuantity,
                        request.Quantity,
                        command.Percent
                    );
                    updates.Add((line, request.Quantity, amount));
                    lineResults.Add(
                        new OrderLineRefundLineResult(line.OrderLineId, request.Quantity, amount)
                    );
                }

                total = updates.Sum(u => u.Amount);
                var plan = total > 0m
                    ? await orderRefunds.BuildPlanForAmountAsync(
                        order.OrderId,
                        total,
                        cancellationToken
                    )
                    : null;

                foreach (var (line, units, amount) in updates)
                {
                    var updated = await db
                        .OrderLines.Where(l =>
                            l.OrderLineId == line.OrderLineId
                            && l.RefundedQuantity == line.RefundedQuantity
                            && l.RefundedAmount == line.RefundedAmount
                            && l.QuantityFulfilled == line.QuantityFulfilled
                            && l.RefundedAmount + amount <= l.PaidAmount
                        )
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(l => l.RefundedAmount, l => l.RefundedAmount + amount)
                                    .SetProperty(
                                        l => l.RefundedQuantity,
                                        l => l.RefundedQuantity + units
                                    ),
                            cancellationToken
                        );
                    if (updated == 0)
                        throw new ConflictAppException(Messages.Refund_LineChanged);
                }

                if (plan is null)
                    return;

                reservations = await orderRefunds.ReserveAsync(
                    plan,
                    command.PerformedByUserId,
                    command.Note,
                    cancellationToken
                );
                await orderRefunds.ReverseLoyaltyForRefundAsync(order, total, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        IReadOnlyList<PaymentRefundResult> transactions =
            reservations.Count == 0
                ? []
                : await orderRefunds.ExecuteAsync(reservations, cancellationToken);
        return new OrderLineRefundResult(total, lineResults, transactions);
    }
}
