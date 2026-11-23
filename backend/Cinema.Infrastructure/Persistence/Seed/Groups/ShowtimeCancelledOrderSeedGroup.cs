using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class ShowtimeCancelledOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-showtime-cancelled";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        await SeedGatewayAsync(c);
        await SeedCashAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.ShowtimeCancelledCashPending,
                SeedWhen.Cancelled,
                null,
                2,
                OrderSeedCombos.Solo,
                1,
                SeedEmails.Employee,
                "Phan Thị Lệ",
                "0932145876",
                TimeSpan.FromDays(2)
            ),
            refundConfirmed: false
        );
        await SeedCashAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.ShowtimeCancelledCashRefunded,
                SeedWhen.Cancelled,
                null,
                2,
                null,
                0,
                SeedEmails.ShiftLead,
                "Võ Minh Quân",
                "0905671234",
                TimeSpan.FromDays(2)
            ),
            refundConfirmed: true
        );
    }

    private static Task SeedGatewayAsync(OrderSeedContext c)
    {
        var spec = new OnlineSpec(
            OrderSeedCodes.ShowtimeCancelled,
            SeedEmails.CustomerNew,
            SeedWhen.Cancelled,
            null,
            2,
            OrderSeedCombos.Solo,
            1,
            (_, reference) => reference.AddDays(-2)
        );
        return c.SeedCancelledOnlineAsync(
            spec,
            CancelReason.ShowtimeCancelled,
            CancelledBy.Admin,
            c.Now.AddDays(-1),
            100m,
            c.AdminUserId,
            OrderSeedContext.ShowtimeCancelledNote(spec.Code)
        );
    }

    private static async Task SeedCashAsync(
        OrderSeedContext c,
        CounterSpec spec,
        bool refundConfirmed
    )
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftCounterAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(2);
        var cancelledAt = c.Now.AddDays(-1);
        var charge = OrderSeedKit.AddCashCharge(
            order,
            order.FinalAmount,
            OrderSeedKit.RoundUpTo(order.FinalAmount, 50_000m),
            paidAt,
            draft.Staff!.UserId
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: false);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);

        var note = OrderSeedContext.ShowtimeCancelledNote(spec.Code);
        if (refundConfirmed)
            OrderSeedKit.AddRefund(
                order,
                charge,
                order.FinalAmount,
                cancelledAt,
                c.AdminUserId,
                note,
                1
            );
        else
            OrderSeedKit.AddPendingCashRefund(
                order,
                charge,
                order.FinalAmount,
                cancelledAt,
                note,
                1
            );

        OrderSeedKit.MarkCancelled(
            order,
            CancelReason.ShowtimeCancelled,
            CancelledBy.Admin,
            cancelledAt
        );

        await c.SaveAsync(order, null, []);
    }
}
