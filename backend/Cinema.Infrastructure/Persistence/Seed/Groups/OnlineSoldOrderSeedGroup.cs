using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class OnlineSoldOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-online-sold";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        await SeedConfirmedAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineConfirmedTier,
                SeedEmails.Customer3,
                SeedWhen.Tomorrow,
                RoomCatalogSeedGroup.ScreenType3D,
                3,
                OrderSeedCombos.Couple,
                1,
                (_, reference) => reference.AddHours(-6)
            ),
            checkedIn: false
        );
        await SeedConfirmedAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineConfirmedImax,
                SeedEmails.Customer4,
                SeedWhen.Tomorrow,
                RoomCatalogSeedGroup.ScreenTypeImax,
                2,
                OrderSeedCombos.Premium,
                1,
                (_, reference) => reference.AddHours(-3)
            ),
            checkedIn: false
        );
        await SeedConfirmedAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.CheckedIn,
                SeedEmails.Customer1,
                SeedWhen.Past,
                null,
                2,
                OrderSeedCombos.Couple,
                1,
                (showtime, _) => showtime.StartTime.AddHours(-26)
            ),
            checkedIn: true
        );
        await SeedSplitGatewayAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.SplitGateway,
                SeedEmails.Customer2,
                SeedWhen.Upcoming,
                RoomCatalogSeedGroup.ScreenType2D,
                3,
                OrderSeedCombos.Solo,
                2,
                (_, reference) => reference.AddHours(-8)
            )
        );
        await SeedComboOnlyAsync(c);
    }

    private static async Task SeedConfirmedAsync(OrderSeedContext c, OnlineSpec spec, bool checkedIn)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftOnlineAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, order.FinalAmount, paidAt);
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        if (checkedIn)
        {
            OrderSeedKit.CheckIn(
                draft.Tickets,
                draft.Allocation.Showtime.StartTime.AddMinutes(-10)
            );
            OrderSeedKit.FulfillCombos(order);
        }

        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt, []);
    }

    private static async Task SeedSplitGatewayAsync(OrderSeedContext c, OnlineSpec spec)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftOnlineAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        var first = Math.Floor(order.FinalAmount * 0.6m / 1_000m) * 1_000m;
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, first, paidAt);
        OrderSeedKit.AddGatewayCharge(
            order,
            PaymentMethod.Momo,
            order.FinalAmount - first,
            paidAt.AddMinutes(1),
            index: 1
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt.AddMinutes(1), []);
    }

    private static async Task SeedComboOnlyAsync(OrderSeedContext c)
    {
        const string code = OrderSeedCodes.OnlineComboOnly;
        if (await c.ExistsAsync(code))
            return;

        if (
            !c.Buyers.TryGetValue(SeedEmails.Customer2, out var buyer)
            || !c.Combos.TryGetValue(OrderSeedCombos.Premium, out var premium)
            || !c.Combos.TryGetValue(OrderSeedCombos.Solo, out var solo)
        )
        {
            return;
        }

        var order = OrderSeedKit.NewOrder(
            code,
            OrderType.Combo,
            SalesChannel.Online,
            c.Now.AddHours(-2),
            c.Settings.HoldDurationMinutes,
            buyer.UserId,
            buyer.FullName,
            buyer.Phone,
            null
        );
        OrderSeedKit.AddCombo(order, premium, 1, null);
        OrderSeedKit.AddCombo(order, solo, 1, null);
        OrderSeedKit.FinalizeAmounts(order);

        var paidAt = order.CreatedAt.AddMinutes(3);
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, order.FinalAmount, paidAt);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await c.SaveAsync(order, paidAt, []);
    }
}
