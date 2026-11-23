using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class PendingOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-pending";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        await SeedHoldingAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.PendingHold,
                SeedEmails.Customer2,
                SeedWhen.Upcoming,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                OrderSeedCombos.Couple,
                1,
                (_, reference) => reference.AddMinutes(-4)
            )
        );
        await SeedExpiredAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineExpired,
                SeedEmails.Customer1,
                SeedWhen.Upcoming,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                OrderSeedCombos.Solo,
                1,
                (_, reference) => reference.AddHours(-3)
            )
        );
    }

    private static async Task SeedHoldingAsync(OrderSeedContext c, OnlineSpec spec)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftOnlineAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        OrderSeedKit.HoldSeats(order, draft.Tickets);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Reserved, order.CreatedAt);
        order.Status = OrderStatus.Pending;

        await c.SaveAsync(order, null, [], draft.Tickets);
    }

    private static async Task SeedExpiredAsync(OrderSeedContext c, OnlineSpec spec)
    {
        if (await c.ExistsAsync(spec.Code))
            return;

        var draft = await c.DraftOnlineAsync(spec);
        if (draft is null)
            return;

        var order = draft.Order;
        OrderSeedKit.AddFailedGatewayCharge(
            order,
            PaymentMethod.VNPay,
            order.FinalAmount,
            order.CreatedAt.AddMinutes(1)
        );
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Released, order.ExpiresAt!.Value);
        order.Status = OrderStatus.Expired;

        await c.SaveAsync(order, null, []);
    }
}
