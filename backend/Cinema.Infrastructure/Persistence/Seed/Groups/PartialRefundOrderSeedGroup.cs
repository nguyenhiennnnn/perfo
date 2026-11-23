using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class PartialRefundOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-partial-refund";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        var spec = new OnlineSpec(
            OrderSeedCodes.PartialRefund,
            SeedEmails.Customer3,
            SeedWhen.Upcoming,
            null,
            2,
            OrderSeedCombos.Solo,
            3,
            (_, reference) => reference.AddHours(-30)
        );
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftOnlineAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        var charge = OrderSeedKit.AddGatewayCharge(
            order,
            PaymentMethod.VNPay,
            order.FinalAmount,
            paidAt
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        var comboLine = order.Lines.First(l => l.LineType == OrderLineType.Combo);
        var refunds = new List<decimal>();
        for (var step = 1; step <= 2; step++)
        {
            var amount = OrderRefundCalculator.LineRefundAmount(
                comboLine.PaidAmount,
                comboLine.Quantity,
                comboLine.RefundedQuantity,
                1,
                100m
            );
            comboLine.RefundedAmount += amount;
            comboLine.RefundedQuantity += 1;
            OrderSeedKit.AddRefund(
                order,
                charge,
                amount,
                c.Now.AddHours(-8 + (step - 1) * 4),
                c.AdminUserId,
                null,
                step
            );
            refunds.Add(amount);
        }

        await c.SaveAsync(order, paidAt, refunds);
    }
}
