using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class CounterOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-counter";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        await SeedCashAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.CounterCash,
                SeedWhen.Soon,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                OrderSeedCombos.Couple,
                1,
                SeedEmails.Employee,
                "Trần Văn Hùng",
                "0987654321",
                TimeSpan.FromMinutes(40)
            )
        );
        await SeedMixedAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.CounterMixed,
                SeedWhen.Soon,
                null,
                4,
                OrderSeedCombos.Family,
                1,
                SeedEmails.ShiftLead,
                "Nguyễn Thị Mai",
                "0971234567",
                TimeSpan.FromHours(1)
            )
        );
        await SeedComboOnlyAsync(c);
    }

    private static async Task SeedCashAsync(OrderSeedContext c, CounterSpec spec)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftCounterAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(2);
        OrderSeedKit.AddCashCharge(
            order,
            order.FinalAmount,
            OrderSeedKit.RoundUpTo(order.FinalAmount, 50_000m),
            paidAt,
            draft.Staff!.UserId
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt, []);
    }

    private static async Task SeedMixedAsync(OrderSeedContext c, CounterSpec spec)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftCounterAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var staff = draft.Staff!;
        var paidAt = order.CreatedAt.AddMinutes(2);
        var cashAmount = Math.Floor(order.FinalAmount * 0.4m / 10_000m) * 10_000m;
        OrderSeedKit.AddCashCharge(
            order,
            cashAmount,
            OrderSeedKit.RoundUpTo(cashAmount, 50_000m),
            paidAt,
            staff.UserId
        );
        OrderSeedKit.AddCardCharge(
            order,
            order.FinalAmount - cashAmount,
            paidAt.AddMinutes(1),
            staff.UserId,
            index: 1
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt, []);
    }

    private static async Task SeedComboOnlyAsync(OrderSeedContext c)
    {
        const string code = OrderSeedCodes.CounterComboOnly;
        if (await c.ExistsAsync(code))
            return;

        if (
            !c.Staff.TryGetValue(SeedEmails.Employee, out var staff)
            || !c.Combos.TryGetValue(OrderSeedCombos.Couple, out var couple)
            || !c.Combos.TryGetValue(OrderSeedCombos.Caramel, out var caramel)
        )
        {
            c.Logger.LogWarning("Skip seeding order {OrderCode}: missing staff or combos.", code);
            return;
        }

        var order = OrderSeedKit.NewOrder(
            code,
            OrderType.Combo,
            SalesChannel.Counter,
            c.Now.AddHours(-5),
            c.Settings.HoldDurationMinutes,
            null,
            "Lê Hoàng Nam",
            "0965432109",
            staff.EmployeeId
        );
        OrderSeedKit.AddCombo(order, couple, 2, null);
        OrderSeedKit.AddCombo(order, caramel, 1, null);
        OrderSeedKit.FinalizeAmounts(order);

        var paidAt = order.CreatedAt.AddMinutes(1);
        OrderSeedKit.AddCashCharge(
            order,
            order.FinalAmount,
            OrderSeedKit.RoundUpTo(order.FinalAmount, 50_000m),
            paidAt,
            staff.UserId
        );
        OrderSeedKit.FulfillCombos(order);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt, []);
    }
}
