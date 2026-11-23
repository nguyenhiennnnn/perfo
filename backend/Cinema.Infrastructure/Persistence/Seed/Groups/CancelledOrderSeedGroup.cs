using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class CancelledOrderSeedGroup : ISeedGroup
{
    public string Name => "orders-cancelled";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var c = await OrderSeedContext.CreateAsync(context, cancellationToken);
        if (c is null)
            return;

        var spec = new OnlineSpec(
            OrderSeedCodes.OnlineCancelled,
            SeedEmails.Customer2,
            SeedWhen.Tomorrow,
            RoomCatalogSeedGroup.ScreenType2D,
            2,
            null,
            0,
            (_, reference) => reference.AddHours(-20)
        );
        if (!c.Buyers.TryGetValue(spec.CustomerEmail, out var buyer))
            return;

        await c.SeedCancelledOnlineAsync(
            spec,
            CancelReason.CustomerChangedMind,
            CancelledBy.Customer,
            c.Now.AddHours(-1),
            c.Settings.CancelRefundPercent,
            buyer.UserId,
            OrderSeedContext.CancelOrderNote(spec.Code)
        );
    }
}
